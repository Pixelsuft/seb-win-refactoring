﻿/*
 * Copyright (c) 2018 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using SafeExamBrowser.Contracts.Logging;

namespace SafeExamBrowser.Contracts.UserInterface
{
	public interface IRuntimeWindow : ILogObserver, IProgressIndicator, IWindow
	{
		/// <summary>
		/// Hides the progress bar.
		/// </summary>
		void HideProgressBar();

		/// <summary>
		/// Shows the progress bar.
		/// </summary>
		void ShowProgressBar();
	}
}