﻿//------------------------------------------------------------------------------
// <auto-generated>
//     這段程式碼是由工具產生的。
//     執行階段版本:4.0.30319.42000
//
//     對這個檔案所做的變更可能會造成錯誤的行為，而且如果重新產生程式碼，
//     變更將會遺失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace MPLite.Core.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MPLiteTrackDB.json")]
        public string TrackDBPath {
            get {
                return ((string)(this["TrackDBPath"]));
            }
            set {
                this["TrackDBPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".mp3|.wma|.wmv")]
        public string ValidFileType {
            get {
                return ((string)(this["ValidFileType"]));
            }
            set {
                this["ValidFileType"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int TaskPlaybackMode {
            get {
                return ((int)(this["TaskPlaybackMode"]));
            }
            set {
                this["TaskPlaybackMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("New Playlist")]
        public string TaskPlaylist {
            get {
                return ((string)(this["TaskPlaylist"]));
            }
            set {
                this["TaskPlaylist"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int PlaybackMode {
            get {
                return ((int)(this["PlaybackMode"]));
            }
            set {
                this["PlaybackMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00000000-0000-0000-0000-000000000000")]
        public global::System.Guid LastSelectedPlaylistGUID {
            get {
                return ((global::System.Guid)(this["LastSelectedPlaylistGUID"]));
            }
            set {
                this["LastSelectedPlaylistGUID"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MPLiteEventDB.json")]
        public string EventDBPath {
            get {
                return ((string)(this["EventDBPath"]));
            }
            set {
                this["EventDBPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MPLiteTrackDB.json")]
        public string TrackDBName {
            get {
                return ((string)(this["TrackDBName"]));
            }
            set {
                this["TrackDBName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MPLiteEventDB.json")]
        public string EventDBName {
            get {
                return ((string)(this["EventDBName"]));
            }
            set {
                this["EventDBName"] = value;
            }
        }
    }
}
