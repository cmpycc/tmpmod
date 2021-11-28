using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace cmpy.TMP
{
    public static class TMPModifierExtensions
    {
        public static TMPModifier GetModifier(this TMP_Text text)
        {
            if (text.TryGetComponent<TMPModifier>(out TMPModifier modifier)) return modifier;
            else return text.gameObject.AddComponent<TMPModifier>();
        }
    }
}