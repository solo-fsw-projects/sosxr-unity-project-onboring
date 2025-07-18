using UnityEngine;
using UnityEditor;
using System;

namespace OverfortGames.SmartEditorSelection
{
    public class EditorPrefsVariable<T> where T : struct
    {
        private string key;
        private T? cachedValue; // Nullable wrapper to allow null checks for value types
        private T defaultValue; // Nullable wrapper to allow null checks for value types

        public EditorPrefsVariable(string key, T defaultValue)
        {
            this.key = key;
            this.defaultValue = defaultValue;
        }

        public T Value
        {
            get
            {
                if (cachedValue == null)
                {
                    cachedValue = GetValue();
                }

                return cachedValue.Value;
            }
            set
            {
                cachedValue = value;
                SetValue(value);
            }
        }

        private T GetValue()
        {
            Type type = typeof(T);

            if (type == typeof(int))
            {
                return (T)(object)EditorPrefs.GetInt(key, (int)(object)defaultValue);
            }
            else if (type == typeof(float))
            {
                return (T)(object)EditorPrefs.GetFloat(key, (float)(object)defaultValue);
            }
            else if (type == typeof(bool))
            {
                return (T)(object)EditorPrefs.GetBool(key, (bool)(object)defaultValue);
            }
            else if (type.IsEnum)
            {
                return (T)(object)EditorPrefs.GetInt(key, (int)(object)defaultValue);
            }
            else if (type == typeof(Color))
            {
                if (ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(key, ColorUtility.ToHtmlStringRGBA((Color)(object)defaultValue)), out var color))
                {
                    return (T)(object)color;
                }
                else
                {
                    return (T)(object)Color.magenta;
                }
            }
            // Add more types if needed
            else
            {
                throw new NotImplementedException("Type not supported");
            }
        }

        private void SetValue(T value)
        {
            Type type = typeof(T);

            if (type == typeof(int))
            {
                EditorPrefs.SetInt(key, (int)(object)value);
            }
            else if (type == typeof(float))
            {
                EditorPrefs.SetFloat(key, (float)(object)value);
            }
            else if (type == typeof(bool))
            {
                EditorPrefs.SetBool(key, (bool)(object)value);
            }
            else if (type.IsEnum)
            {
                EditorPrefs.SetInt(key, (int)(object)value);
            }
            else if (type == typeof(Color))
            {
                EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA((Color)(object)value));
            }
            // Add more types if needed
            else
            {
                throw new NotImplementedException("Type not supported");
            }
        }
    }
}