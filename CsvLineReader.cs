using System;

using System.Globalization;

namespace opc_stream
{

    /// <summary>
    ///  reads csv-file line-by-line for streaming (avoid placing entire file in memory, to get around x64 limits 
    /// </summary>

    class CsvLineReader
    {
        System.IO.StreamReader streamReader;

        string[] variableNames;
        char separator;
        string dateTimeFormat;
        string timeStamp;

        public CsvLineReader(string fileName, char separator=';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            streamReader = new System.IO.StreamReader(fileName);
            this.separator = separator;
            this.dateTimeFormat = dateTimeFormat;
            variableNames = streamReader.ReadLine().Split(separator);
        }

        public string[]  GetVariableNames()
        {
            return variableNames;
        }

        // 
        /// <summary>
        /// after calling GetLine, this method can return a string with the time stamp as written in the csv-file
        /// </summary>
        /// <returns></returns>
        public string GetTimeStampAtLastLineRead()
        {
            return timeStamp;
        }

        
        /// <summary>
        /// If browsing to find a specific date, then this lightweight version of GetNextLine reads just the date
        /// </summary>
        /// <returns>return null if EOF find</returns>
        public DateTime? GetNextDate()
        {
            string[] lineStr = new string[0];
            while (lineStr.Length == 0 && !streamReader.EndOfStream)
            {
                string currentLine = streamReader.ReadLine();
                lineStr = currentLine.Split(separator);
                lineStr = currentLine.Split(separator);
                if (lineStr.Length > 0)
                {
                    timeStamp = lineStr[0];
                    return DateTime.ParseExact(timeStamp, dateTimeFormat, CultureInfo.InvariantCulture);
                }
            }
            return null;
        }




        /// <summary>
        /// Gets date and values for next line in csv, or empty date and null if EOF
        /// </summary>
        /// <returns></returns>
        public (DateTime, double[]) GetNextLine()
        {
            string[] lineStr = new string[0];
            while (lineStr.Length == 0 && !streamReader.EndOfStream)
            {
                double[] values = new double[variableNames.Length - 1];
                string currentLine = streamReader.ReadLine();
                lineStr = currentLine.Split(separator);
                if (lineStr.Length > 0)
                {
                    // get date
                    timeStamp = lineStr[0];
                    DateTime date = DateTime.ParseExact(timeStamp, dateTimeFormat, CultureInfo.InvariantCulture);
                    // get values
                    for (int k = 1; k < Math.Min(lineStr.Length, values.Length); k++)
                    {
                        if (lineStr[k].Length > 0)
                            RobustParseDouble(lineStr[k], out values[k-1]);
                    }
                    return (date, values);
                }
            }
            return (new DateTime(), null);// should not happen unless EOF
        }


        /// <summary>
        ///  Loading string data into a double value.
        /// </summary>
        /// <param name="str">the string to be parsed</param>
        /// <param name="value">(output) is the value of the parsed double(if successfully parsed)</param>
        /// <returns>The method returns true if succesful, otherwise it returns false.</returns>
        static private bool RobustParseDouble(string str, out double value)
        {
            str = str.Replace(',', '.');
            bool abletoParseVal = false;
            if (Double.TryParse(str, System.Globalization.NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value))
                abletoParseVal = true;
            else if (Double.TryParse(str, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out value))
                abletoParseVal = true;
            else if (Double.TryParse(str, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture,
                out value))
                abletoParseVal = true;
            return abletoParseVal;
        }


    }
}
