using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace genscript
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0"), DebuggerNonUserCode, CompilerGenerated]
	internal class strings
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(strings.resourceMan, null))
				{
					ResourceManager resourceManager = new ResourceManager("genscript.strings", typeof(strings).Assembly);
					strings.resourceMan = resourceManager;
				}
				return strings.resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return strings.resourceCulture;
			}
			set
			{
				strings.resourceCulture = value;
			}
		}

		internal static string version
		{
			get
			{
				return strings.ResourceManager.GetString("version", strings.resourceCulture);
			}
		}

		internal strings()
		{
		}
	}
}
