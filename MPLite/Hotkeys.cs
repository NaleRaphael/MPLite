using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace MPLite
{
    using DataControl = Core.DataControl;

    public class Hotkeys : List<Hotkey>
    {
        public void Save()
        {
            DataControl.SaveData<Hotkeys>(MPLiteSetting.HotkeysSettingPath, this, true, true);
        }

        public void Save(Hotkey hotkey)
        {
            int targetIndex = this.FindIndex(x => x.Name == hotkey.Name);
            if (targetIndex == -1) return;

            this[targetIndex] = hotkey;
            this.Save();
        }

        public static Hotkeys Load()
        {
            Hotkeys result = DataControl.ReadFromJson<Hotkeys>(MPLiteSetting.HotkeysSettingPath, true);
            if (result == null)
            {
                result = new Hotkeys();
                result.Initialize();
            }
            return result;
        }

        public void Initialize()
        {
            foreach (string name in MPLiteSetting.HotkeyNames)
            {
                this.Add(new Hotkey { Name = name });
            }
        }

        public bool CheckNameConfilct(Hotkey target)
        {
            bool result = false;

            foreach (Hotkey hk in this)
            {
                if (hk.Name == target.Name)
                    continue;

                if (hk.SystemKey == target.SystemKey && hk.NormalKey == target.NormalKey)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public string FindName(KeyEventArgs e, ModifierKeys currentModifier)
        {
            Hotkey parsedHotkey = new Hotkey();
            parsedHotkey.Set(e, currentModifier);

            foreach (Hotkey hk in this)
            {
                if (hk.SystemKey == parsedHotkey.SystemKey && hk.NormalKey == parsedHotkey.NormalKey)
                    return hk.Name;
            }

            return null;
        }

        private bool CheckContent()
        {
            bool isComplete = true;

            List<string> hotkeyNames = new List<string>();
            foreach (Hotkey hk in this)
            {
                hotkeyNames.Add(hk.Name);
            }

            foreach (string hkName in hotkeyNames)
            {
                if (!MPLiteSetting.HotkeyNames.Contains(hkName))
                {
                    isComplete = false;
                    break;
                }
            }

            return isComplete;
        }
    }

    public class Hotkey
    {
        private ModifierKeys _systemKey = ModifierKeys.None;
        private Key _normalKey = Key.None;

        public ModifierKeys SystemKey
        {
            get { return _systemKey; }
            set { _systemKey = value; }
        }
        public Key NormalKey
        {
            get { return _normalKey; }
            set { _normalKey = value; }
        }

        public string Name { get; set; }

        public Hotkey()
        {
            SystemKey = ModifierKeys.None;
            NormalKey = Key.None;
        }

        public void Set(KeyEventArgs e, ModifierKeys currentModifier)
        {
            GetSystemKeyAndNormalKey(e, currentModifier, out _systemKey, out _normalKey, _systemKey, _normalKey);
        }

        public bool TrySet(string source, char delimeter)
        {
            string[] keys;
            try
            {
                keys = source.Trim().Split(delimeter);
            }
            catch
            {
                throw;
            }

            if (keys.Length > 2 || keys.Length == 0)
            {
                return false;
            }
            
            if (keys.Length == 2)
            {
                if (!Enum.TryParse(keys[0], out _systemKey))
                    return false;
                if (!Enum.TryParse(keys[1], out _normalKey))
                    return false;
            }
            else
            {
                if (!Enum.TryParse(keys[0], out _normalKey))
                    return false;
            }

            return true;
        }

        public static void GetSystemKeyAndNormalKey(KeyEventArgs e, ModifierKeys currentModifier, out ModifierKeys sKey, out Key nKey,
            ModifierKeys sKeyDefault = ModifierKeys.None, Key nKeyDefault = Key.None)
        {
            bool isModifierKeyPressed = currentModifier != ModifierKeys.None;
            sKey = currentModifier;
            nKey = isModifierKeyPressed ? nKeyDefault : Key.None;

            Console.WriteLine(string.Format("{0}, {1}", e.SystemKey, e.Key));

            switch (e.SystemKey)
            {
                case Key.None:
                    nKey = e.Key;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    sKey = currentModifier;
                    break;
                default:
                    nKey = e.SystemKey;
                    break;
            }
        }
    }
}
