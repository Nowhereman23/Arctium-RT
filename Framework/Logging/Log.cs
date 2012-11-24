/*
 * Copyright (C) 2012 Arctium <http://>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using XMedia = Windows.UI.Xaml.Media;

namespace Framework.Logging
{
    public sealed class Log
    {
        public static TextBlock CurrentBlock { get; set; }

        static public void Message()
        {
            SetLogger(LogType.DEFAULT, "");
        }

        static public void Message(LogType type, string text, params object[] args)
        {
            SetLogger(type, text, args);
        }

        static void SetLogger(LogType type, string text, params object[] args)
        {
            var solidColor = new XMedia.SolidColorBrush();

            switch (type)
            {
                case LogType.NORMAL:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;

                    text = text.Insert(0, "System: ");
                    break;
                case LogType.ERROR:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;

                    text = text.Insert(0, "Error: ");
                    break;
                case LogType.DUMP:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;
                    break;
                case LogType.INIT:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;
                    break;
                case LogType.MISC:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;
                    break;
                case LogType.CMD:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;
                    break;
                case LogType.DEBUG:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;
                    break;
                default:
                    solidColor.Color = Colors.Green;
                    CurrentBlock.Foreground = solidColor;
                    break;
            }

            StringBuilder logText = new StringBuilder();

            if (type.Equals(LogType.INIT) | type.Equals(LogType.DEFAULT))
                logText.AppendFormat(text, args);
            else if (type.Equals(LogType.DUMP) || type.Equals(LogType.CMD))
                logText.AppendFormat(text, args);
            else
                logText.AppendFormat("[" + DateTime.Now.ToString() + "] " + text, args);

            CurrentBlock.Text += logText.ToString() + "\n";
        }
    }
}
