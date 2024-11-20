﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MultiFactor.IIS.Adapter.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MultiFactor.IIS.Adapter.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!DOCTYPE html&gt;
        ///&lt;html&gt;
        ///    &lt;head&gt;
        ///        &lt;meta name=&quot;viewport&quot; content=&quot;width=device-width, initial-scale=1&quot;&gt;
        ///        &lt;title&gt;Finishing 2FA...&lt;/title&gt;
        ///    &lt;/head&gt;
        ///    &lt;body&gt;
        ///        &lt;div&gt;Finishing 2FA...&lt;/div&gt;
        ///        &lt;script type=&quot;text/javascript&quot;&gt;
        ///            document.addEventListener(&quot;DOMContentLoaded&quot;, (event) =&gt; {
        ///                const multifactorCookie = &quot;%MULTIFACTOR_COOKIE%&quot;;
        ///                const domain = window.location.hostname;
        ///                document.cookie = `multifactor=${multifa [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string complete_2fa_html {
            get {
                return ResourceManager.GetString("complete_2fa.html", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!DOCTYPE html&gt;
        ///&lt;html&gt;
        ///    &lt;head&gt;
        ///        &lt;meta name=&quot;viewport&quot; content=&quot;width=device-width, initial-scale=1&quot;&gt;
        ///        &lt;title&gt;Finishing Logout...&lt;/title&gt;
        ///    &lt;/head&gt;
        ///    &lt;body&gt;
        ///        &lt;div&gt;Finishing Logout...&lt;/div&gt;
        ///        &lt;script type=&quot;text/javascript&quot;&gt;
        ///            document.addEventListener(&quot;DOMContentLoaded&quot;, (event) =&gt; {
        ///                const domain = window.location.hostname;
        ///                document.cookie = `multifactor=; path=/; domain=${domain}; secure; expires=${new Date(Date.now() - (1 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string complete_logout_html {
            get {
                return ResourceManager.GetString("complete_logout.html", resourceCulture);
            }
        }
    }
}
