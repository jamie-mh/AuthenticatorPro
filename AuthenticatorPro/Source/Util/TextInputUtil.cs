using System;
using System.Collections.Generic;
using Google.Android.Material.TextField;

namespace AuthenticatorPro.Util
{
    internal static class TextInputUtil
    {
        public static void EnableAutoErrorClear(IEnumerable<TextInputLayout> layouts)
        {
            foreach(var layout in layouts)
                EnableAutoErrorClear(layout);
        }
        
        public static void EnableAutoErrorClear(TextInputLayout layout)
        {
            layout.EditText.TextChanged += delegate
            {
                if(!String.IsNullOrEmpty(layout.Error))
                    layout.Error = null;
            };
        }
    }
}