// <copyright file="Localization.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Colossal.Localization;
    using Game.SceneFlow;

    /// <summary>
    /// Translation handling.
    /// </summary>
    public static class Localization
    {
        /// <summary>
        /// Loads translations from tab-separated l10n file.
        /// </summary>
        public static void LoadTranslations()
        {
            try
            {
                string translationFile = Path.Combine(UIFileUtils.AssemblyPath, "l10n.csv");

                if (File.Exists(translationFile))
                {
                    // Parse file.
                    IEnumerable<string[]> fileLines = File.ReadAllLines(translationFile).Select(x => x.Split('\t'));

                    // Iterate through each game locale.
                    foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
                    {
                        try
                        {
                            // Find matching column in file.
                            int valueColumn = Array.IndexOf(fileLines.First(), localeID);

                            // Make sure a valid column has been found (column 0 is the translation key).
                            if (valueColumn > 0)
                            {
                                // Add translations to game locales.
                                MemorySource language = new (fileLines.Skip(1).ToDictionary(x => x[0], x => x.ElementAtOrDefault(valueColumn)));
                                GameManager.instance.localizationManager.AddSource(localeID, language);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e, "exception reading localization for locale ", localeID);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "exception reading localization file");
            }
        }
    }
}
