using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    /// <summary>
    /// Information about current VPS session
    /// </summary>
    public class SessionInfo
    {
        public string Id;
        public int ResponsesCount;
        public int SuccessLocalizationCount;
        public int FailLocalizationCount;
        public int SuccessLocalizationInRow;

        public SessionInfo()
        {
            Id = System.Guid.NewGuid().ToString();
            ResponsesCount = 0;
            SuccessLocalizationCount = 0;
            FailLocalizationCount = 0;
            SuccessLocalizationInRow = 0;
        }

        public void SuccessLocalization()
        {
            ResponsesCount += 1;
            SuccessLocalizationCount += 1;
            SuccessLocalizationInRow += 1;
        }

        public void FailLocalization()
        {
            ResponsesCount += 1;
            FailLocalizationCount += 1;
            SuccessLocalizationInRow = 0;
        }
    }
}
