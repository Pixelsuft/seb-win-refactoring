﻿/*
 * Copyright (c) 2025 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace SafeExamBrowser.UserInterface.Contracts.Browser.Events
{
	/// <summary>
	/// Indicates a load error for a browser request.
	/// </summary>
	public delegate void LoadFailedEventHandler(int errorCode, string errorText, bool isMainRequest, string url);
}
