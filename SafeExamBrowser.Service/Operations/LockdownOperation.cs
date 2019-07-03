﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using SafeExamBrowser.Contracts.Core.OperationModel;
using SafeExamBrowser.Contracts.Lockdown;
using SafeExamBrowser.Contracts.Logging;

namespace SafeExamBrowser.Service.Operations
{
	internal class LockdownOperation : SessionOperation
	{
		private IFeatureConfigurationBackup backup;
		private IFeatureConfigurationFactory factory;
		private ILogger logger;
		private Guid groupId;

		public LockdownOperation(
			IFeatureConfigurationBackup backup,
			IFeatureConfigurationFactory factory,
			ILogger logger,
			SessionContext sessionContext) : base(sessionContext)
		{
			this.backup = backup;
			this.factory = factory;
			this.logger = logger;
		}

		public override OperationResult Perform()
		{
			groupId = Guid.NewGuid();

			var success = true;
			var sid = Context.Configuration.UserSid;
			var userName = Context.Configuration.UserName;
			var configurations = new []
			{
				(factory.CreateChangePasswordConfiguration(groupId, sid, userName), Context.Configuration.Settings.Service.DisablePasswordChange),
				(factory.CreateChromeNotificationConfiguration(groupId, sid, userName), Context.Configuration.Settings.Service.DisableChromeNotifications),
				(factory.CreateEaseOfAccessConfiguration(groupId), Context.Configuration.Settings.Service.DisableEaseOfAccessOptions),
				(factory.CreateLockWorkstationConfiguration(groupId, sid, userName), Context.Configuration.Settings.Service.DisableUserLock),
				(factory.CreateNetworkOptionsConfiguration(groupId), Context.Configuration.Settings.Service.DisableNetworkOptions),
				(factory.CreatePowerOptionsConfiguration(groupId), Context.Configuration.Settings.Service.DisablePowerOptions),
				(factory.CreateRemoteConnectionConfiguration(groupId), Context.Configuration.Settings.Service.DisableRemoteConnections),
				(factory.CreateSignoutConfiguration(groupId, sid, userName), Context.Configuration.Settings.Service.DisableSignout),
				(factory.CreateSwitchUserConfiguration(groupId), Context.Configuration.Settings.Service.DisableUserSwitch),
				(factory.CreateTaskManagerConfiguration(groupId, sid, userName), Context.Configuration.Settings.Service.DisableTaskManager),
				(factory.CreateVmwareOverlayConfiguration(groupId, sid, userName), Context.Configuration.Settings.Service.DisableVmwareOverlay),
				(factory.CreateWindowsUpdateConfiguration(groupId), Context.Configuration.Settings.Service.DisableWindowsUpdate)
			};

			logger.Info($"Attempting to perform lockdown (feature configuration group: {groupId})...");

			foreach (var (configuration, disable) in configurations)
			{
				success &= SetConfiguration(configuration, disable);

				if (!success)
				{
					break;
				}
			}

			if (success)
			{
				logger.Info("Lockdown successful.");
			}
			else
			{
				logger.Error("Lockdown was not successful!");
			}

			return success ? OperationResult.Success : OperationResult.Failed;
		}

		public override OperationResult Revert()
		{
			logger.Info($"Attempting to revert lockdown (feature configuration group: {groupId})...");

			var configurations = backup.GetBy(groupId);
			var success = true;

			foreach (var configuration in configurations)
			{
				var restored = configuration.Restore();

				if (restored)
				{
					backup.Delete(configuration);
				}
				else
				{
					logger.Error($"Failed to restore {configuration}!");
					success = false;
				}
			}

			if (success)
			{
				logger.Info("Lockdown reversion successful.");
			}
			else
			{
				logger.Warn("Lockdown reversion was not successful!");
			}

			return success ? OperationResult.Success : OperationResult.Failed;
		}

		private bool SetConfiguration(IFeatureConfiguration configuration, bool disable)
		{
			var success = false;

			configuration.Initialize();
			backup.Save(configuration);

			if (disable)
			{
				success = configuration.DisableFeature();
			}
			else
			{
				success = configuration.EnableFeature();
			}

			if (success)
			{
				configuration.Monitor();
			}
			else
			{
				logger.Error($"Failed to configure {configuration}!");
			}

			return success;
		}
	}
}
