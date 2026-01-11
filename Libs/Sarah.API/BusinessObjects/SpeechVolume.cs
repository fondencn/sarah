using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.BusinessObjects
{

    /// <summary>
    /// Sprachausgabe - Lautstärke
    /// </summary>
    public enum SpeechVolume
    {
        /// <summary>
        /// Normal
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 20% Lauter als normal
        /// </summary>
        Louder = 1,
        /// <summary>
        /// 40% lauter als normal
        /// </summary>
        VeryLoud = 2,
        /// <summary>
        /// 20% leiser als normal
        /// </summary>
        Quieter = 3
    }
}
