﻿/*
 * Copyright (c) 2025 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

namespace SafeExamBrowser.Settings.Monitoring
{
	/// <summary>
	/// Defines all settings related to the display configuration monitoring.
	/// </summary>
	[Serializable]
	public class DisplaySettings
	{
		/// <summary>
		/// Defines the number of allowed displays.
		/// </summary>
		public int AllowedDisplays { get; set; }

		/// <summary>
		/// Determines whether the display(s) will remain always on or not. This does not prevent the operating system from entering sleep mode or
		/// standby, see <see cref="System.SystemSettings.AlwaysOn"/>.
		/// </summary>
		public bool AlwaysOn { get; set; }

		/// <summary>
		/// Determines whether any display configuration may be allowed when the configuration can't be verified due to an error.
		/// </summary>
		public bool IgnoreError { get; set; }

		/// <summary>
		/// Determines whether only an internal display may be used.
		/// </summary>
		public bool InternalDisplayOnly { get; set; }
	}
}
