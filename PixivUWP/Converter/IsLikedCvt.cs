﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace PixivUWP.Converter
{
    public class IsLikedCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var v = value as bool?;
            if (v == true)
                return "";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}