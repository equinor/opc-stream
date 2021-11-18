using System;
using System.IO;
using System.Globalization;

namespace opc_stream
{

    /// <summary>
    ///  reads csv-file line-by-line for streaming (avoid placing entire file in memory, to get around x64 limits 
    /// </summary>

    class CsvLineReader
    {
        StreamReader streamReader;

        string[] variableNames;
        char separator;
        string dateTimeFormat;
        string timeStamp;
        int currentLine;

        public CsvLineReader(string fileName, char separator = ';', string dateTimeFormat = "yyyy-MM-dd HH:mm:ss")
        {
            streamReader = new System.IO.StreamReader(fileName);
            this.separator = separator;
            this.dateTimeFormat = dateTimeFormat;
            variableNames = streamReader.ReadLine().Split(separator);
            currentLine = 0;
        }

        public string[] GetVariableNames()
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
            currentLine++;
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

        public long GetActualPosition()
        {
            StreamReader reader = streamReader;
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField;

            // The current buffer of decoded characters
            char[] charBuffer = (char[])reader.GetType().InvokeMember("charBuffer", flags, null, reader, null);

            // The index of the next char to be read from charBuffer
            int charPos = (int)reader.GetType().InvokeMember("charPos", flags, null, reader, null);

            // The number of decoded chars presently used in charBuffer
            int charLen = (int)reader.GetType().InvokeMember("charLen", flags, null, reader, null);

            // The current buffer of read bytes (byteBuffer.Length = 1024; this is critical).
            byte[] byteBuffer = (byte[])reader.GetType().InvokeMember("byteBuffer", flags, null, reader, null);

            // The number of bytes read while advancing reader.BaseStream.Position to (re)fill charBuffer
            int byteLen = (int)reader.GetType().InvokeMember("byteLen", flags, null, reader, null);

            // The number of bytes the remaining chars use in the original encoding.
            int numBytesLeft = reader.CurrentEncoding.GetByteCount(charBuffer, charPos, charLen - charPos);

            // For variable-byte encodings, deal with partial chars at the end of the buffer
            int numFragments = 0;
            if (byteLen > 0 && !reader.CurrentEncoding.IsSingleByte)
            {
                if (reader.CurrentEncoding.CodePage == 65001) // UTF-8
                {
                    byte byteCountMask = 0;
                    while ((byteBuffer[byteLen - numFragments - 1] >> 6) == 2) // if the byte is "10xx xxxx", it's a continuation-byte
                        byteCountMask |= (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask
                    if ((byteBuffer[byteLen - numFragments - 1] >> 6) == 3) // if the byte is "11xx xxxx", it starts a multi-byte char.
                        byteCountMask |= (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask
                                                                      // see if we found as many bytes as the leading-byte says to expect
                    if (numFragments > 1 && ((byteBuffer[byteLen - numFragments] >> 7 - numFragments) == byteCountMask))
                        numFragments = 0; // no partial-char in the byte-buffer to account for
                }
                else if (reader.CurrentEncoding.CodePage == 1200) // UTF-16LE
                {
                    if (byteBuffer[byteLen - 1] >= 0xd8) // high-surrogate
                        numFragments = 2; // account for the partial character
                }
                else if (reader.CurrentEncoding.CodePage == 1201) // UTF-16BE
                {
                    if (byteBuffer[byteLen - 2] >= 0xd8) // high-surrogate
                        numFragments = 2; // account for the partial character
                }
            }
            return reader.BaseStream.Position - numBytesLeft - numFragments;
        }

        public void SetActualPosition(long desiredPosition)
        {
            streamReader.BaseStream.Position = desiredPosition;
        }


        public int GetCurrentLineNumber()
        {
            return currentLine;
        }

        /// <summary>
        /// Gets date and values for next line in csv, or empty date and null if EOF
        /// </summary>
        /// <returns></returns>
        public (DateTime, double[]) GetNextLine()
        {
            currentLine++;

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
