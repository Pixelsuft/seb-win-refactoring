﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SafeExamBrowser.Contracts.Configuration;
using SafeExamBrowser.Contracts.Configuration.Settings;
using SafeExamBrowser.Contracts.Core.OperationModel;
using SafeExamBrowser.Contracts.Lockdown;
using SafeExamBrowser.Contracts.Logging;
using SafeExamBrowser.Service.Operations;

namespace SafeExamBrowser.Service.UnitTests.Operations
{
	[TestClass]
	public class LockdownOperationTests
	{
		private Mock<IFeatureConfigurationBackup> backup;
		private Mock<IFeatureConfigurationFactory> factory;
		private Mock<ILogger> logger;
		private Settings settings;
		private SessionContext sessionContext;
		private LockdownOperation sut;

		[TestInitialize]
		public void Initialize()
		{
			backup = new Mock<IFeatureConfigurationBackup>();
			factory = new Mock<IFeatureConfigurationFactory>();
			logger = new Mock<ILogger>();
			settings = new Settings();
			sessionContext = new SessionContext { Configuration = new ServiceConfiguration { Settings = settings } };

			sut = new LockdownOperation(backup.Object, factory.Object, logger.Object, sessionContext);
		}

		[TestMethod]
		public void Perform_MustSetConfigurationsCorrectly()
		{
			var configuration = new Mock<IFeatureConfiguration>();
			var count = typeof(IFeatureConfigurationFactory).GetMethods().Where(m => m.Name.StartsWith("Create")).Count();

			configuration.SetReturnsDefault(true);
			factory.SetReturnsDefault(configuration.Object);
			settings.Service.DisableChromeNotifications = true;
			settings.Service.DisablePowerOptions = true;
			settings.Service.DisableSignout = true;

			var result = sut.Perform();

			backup.Verify(b => b.Save(It.Is<IFeatureConfiguration>(c => c == configuration.Object)), Times.Exactly(count));
			configuration.Verify(c => c.DisableFeature(), Times.Exactly(3));
			configuration.Verify(c => c.EnableFeature(), Times.Exactly(count - 3));
			configuration.Verify(c => c.Monitor(), Times.Exactly(count));

			Assert.AreEqual(OperationResult.Success, result);
		}

		[TestMethod]
		public void Perform_MustUseSameGroupForAllConfigurations()
		{
			var configuration = new Mock<IFeatureConfiguration>();
			var groupId = default(Guid);

			configuration.SetReturnsDefault(true);
			factory.Setup(f => f.CreateChromeNotificationConfiguration(It.IsAny<Guid>())).Returns(configuration.Object).Callback<Guid>(id => groupId = id);
			factory.SetReturnsDefault(configuration.Object);

			sut.Perform();

			factory.Verify(f => f.CreateChromeNotificationConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateEaseOfAccessConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateNetworkOptionsConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreatePasswordChangeConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreatePowerOptionsConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateRemoteConnectionConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateSignoutConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateTaskManagerConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateUserLockConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateUserSwitchConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateVmwareOverlayConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
			factory.Verify(f => f.CreateWindowsUpdateConfiguration(It.Is<Guid>(id => id == groupId)), Times.Once);
		}

		[TestMethod]
		public void Perform_MustImmediatelyAbortOnFailure()
		{
			var configuration = new Mock<IFeatureConfiguration>();
			var count = typeof(IFeatureConfigurationFactory).GetMethods().Where(m => m.Name.StartsWith("Create")).Count();
			var counter = 0;
			var offset = 3;

			configuration.Setup(c => c.EnableFeature()).Returns(() => ++counter < count - offset);
			factory.SetReturnsDefault(configuration.Object);

			var result = sut.Perform();

			backup.Verify(b => b.Save(It.Is<IFeatureConfiguration>(c => c == configuration.Object)), Times.Exactly(count - offset));
			configuration.Verify(c => c.DisableFeature(), Times.Never);
			configuration.Verify(c => c.EnableFeature(), Times.Exactly(count - offset));
			configuration.Verify(c => c.Monitor(), Times.Exactly(count - offset - 1));

			Assert.AreEqual(OperationResult.Failed, result);
		}

		[TestMethod]
		public void Revert_MustRestoreConfigurationsCorrectly()
		{
			var configuration1 = new Mock<IFeatureConfiguration>();
			var configuration2 = new Mock<IFeatureConfiguration>();
			var configuration3 = new Mock<IFeatureConfiguration>();
			var configuration4 = new Mock<IFeatureConfiguration>();
			var configurations = new List<IFeatureConfiguration>
			{
				configuration1.Object,
				configuration2.Object,
				configuration3.Object,
				configuration4.Object
			};

			backup.Setup(b => b.GetBy(It.IsAny<Guid>())).Returns(configurations);
			configuration1.Setup(c => c.Restore()).Returns(true);
			configuration2.Setup(c => c.Restore()).Returns(true);
			configuration3.Setup(c => c.Restore()).Returns(true);
			configuration4.Setup(c => c.Restore()).Returns(true);

			var result = sut.Revert();

			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration1.Object)), Times.Once);
			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration2.Object)), Times.Once);
			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration3.Object)), Times.Once);
			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration4.Object)), Times.Once);

			configuration1.Verify(c => c.DisableFeature(), Times.Never);
			configuration1.Verify(c => c.EnableFeature(), Times.Never);
			configuration1.Verify(c => c.Restore(), Times.Once);

			configuration2.Verify(c => c.DisableFeature(), Times.Never);
			configuration2.Verify(c => c.EnableFeature(), Times.Never);
			configuration2.Verify(c => c.Restore(), Times.Once);

			configuration3.Verify(c => c.DisableFeature(), Times.Never);
			configuration3.Verify(c => c.EnableFeature(), Times.Never);
			configuration3.Verify(c => c.Restore(), Times.Once);

			configuration4.Verify(c => c.DisableFeature(), Times.Never);
			configuration4.Verify(c => c.EnableFeature(), Times.Never);
			configuration4.Verify(c => c.Restore(), Times.Once);

			Assert.AreEqual(OperationResult.Success, result);
		}

		[TestMethod]
		public void Revert_MustContinueToRevertOnFailure()
		{
			var configuration1 = new Mock<IFeatureConfiguration>();
			var configuration2 = new Mock<IFeatureConfiguration>();
			var configuration3 = new Mock<IFeatureConfiguration>();
			var configuration4 = new Mock<IFeatureConfiguration>();
			var configurations = new List<IFeatureConfiguration>
			{
				configuration1.Object,
				configuration2.Object,
				configuration3.Object,
				configuration4.Object
			};

			backup.Setup(b => b.GetBy(It.IsAny<Guid>())).Returns(configurations);
			configuration1.Setup(c => c.Restore()).Returns(true);
			configuration2.Setup(c => c.Restore()).Returns(false);
			configuration3.Setup(c => c.Restore()).Returns(false);
			configuration4.Setup(c => c.Restore()).Returns(true);

			var result = sut.Revert();

			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration1.Object)), Times.Once);
			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration2.Object)), Times.Never);
			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration3.Object)), Times.Never);
			backup.Verify(b => b.Delete(It.Is<IFeatureConfiguration>(c => c == configuration4.Object)), Times.Once);

			configuration1.Verify(c => c.DisableFeature(), Times.Never);
			configuration1.Verify(c => c.EnableFeature(), Times.Never);
			configuration1.Verify(c => c.Restore(), Times.Once);

			configuration2.Verify(c => c.DisableFeature(), Times.Never);
			configuration2.Verify(c => c.EnableFeature(), Times.Never);
			configuration2.Verify(c => c.Restore(), Times.Once);

			configuration3.Verify(c => c.DisableFeature(), Times.Never);
			configuration3.Verify(c => c.EnableFeature(), Times.Never);
			configuration3.Verify(c => c.Restore(), Times.Once);

			configuration4.Verify(c => c.DisableFeature(), Times.Never);
			configuration4.Verify(c => c.EnableFeature(), Times.Never);
			configuration4.Verify(c => c.Restore(), Times.Once);

			Assert.AreEqual(OperationResult.Failed, result);
		}
	}
}