using System;
using System.ComponentModel.DataAnnotations;

namespace BattleshipContestFunc
{
    public class AbsoluteUriAttribute : ValidationAttribute
    {
        public AbsoluteUriAttribute() { }

        public override bool IsValid(object? objValue)
        {
            if (objValue == null) return true;
            if (objValue is string value) return Uri.TryCreate(value, UriKind.Absolute, out var _);
            return false;
        }
    }
}
