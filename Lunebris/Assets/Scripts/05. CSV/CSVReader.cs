// System
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;

// Unity
using UnityEngine;

public class CSVReader
{
    private string filePath;
    private string filePath_csv;

    public string ReadHeader(string fileName)
    {
        if (File.Exists(fileName))
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line = reader.ReadLine();
                return line;
            }
        }
        return null;
    }

    public T ReadCSVFile<T>(string fileName) where T : new()
    {
        filePath = Path.Combine(Application.dataPath, "Resources/Databases", fileName);
        filePath_csv = filePath + ".csv";

        if (File.Exists(filePath_csv))
        {
            using (StreamReader reader = new StreamReader(filePath_csv))
            {
                bool headerSkipped = false;
                string[] headers = null;

                // T�� List�� ���
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = typeof(T).GetGenericArguments()[0];
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (!headerSkipped)
                        {
                            headers = line.Split(',');
                            headerSkipped = true;
                            continue;
                        }

                        string[] values = line.Split(',');
                        var item = Activator.CreateInstance(listType);

                        for (int i = 0; i < headers.Length; i++)
                        {
                            PropertyInfo property = listType.GetProperty(headers[i].Trim());
                            if (property != null && property.CanWrite)
                            {
                                object convertedValue;

                                if (property.PropertyType == typeof(bool))
                                {
                                    string boolValue = values[i].Trim().ToLower();
                                    convertedValue = ConvertToBoolean(boolValue);
                                }
                                else
                                {
                                    convertedValue = Convert.ChangeType(values[i], property.PropertyType);
                                }

                                property.SetValue(item, convertedValue, null);
                            }
                        }

                        list.Add(item);
                    }

                    return (T)(object)list;
                }

                //T�� ���� ��ü�� ���
                else
                {
                    var item = new T();

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (!headerSkipped)
                        {
                            headers = line.Split(',');
                            headerSkipped = true;
                            continue;
                        }

                        string[] values = line.Split(',');

                        for (int i = 0; i < headers.Length; i++)
                        {
                            PropertyInfo property = typeof(T).GetProperty(headers[i]);
                            if (property != null && property.CanWrite)
                            {
                                object convertedValue;

                                if (property.PropertyType == typeof(bool))
                                {
                                    string boolValue = values[i].Trim().ToLower();
                                    convertedValue = ConvertToBoolean(boolValue);
                                }
                                else
                                {
                                    convertedValue = Convert.ChangeType(values[i], property.PropertyType);
                                }

                                property.SetValue(item, convertedValue, null);
                            }
                        }
                    }

                    return item;
                }
            }
        }

        return default(T);
    }

    bool ConvertToBoolean(string boolValue)
    {
        if (boolValue == "true" ||
             boolValue == "1")
        {
            return true;
        }
        else if (boolValue == "false" ||
                  boolValue == "0")
        {
            return false;
        }
        else
        {
            throw new FormatException("��ȿ�� �ο� ���� �ƴմϴ�.");
        }
    }
}
