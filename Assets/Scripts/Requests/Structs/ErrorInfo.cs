using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    public enum ErrorCode
    {
        NO_INTERNET, NO_CAMERA, TRACKING_NOT_AVALIABLE, SERVER_INTERNAL_ERROR, DESERIALIZED_ERROR, VALIDATION_ERROR, LOCALISATION_FAIL, AR_NOT_SUPPORTED
    }

    public class ErrorInfo
    {
        // error type
        public ErrorCode Code;
        // error description from server
        public string Message;
        // bad field's value if it's validation error
        public string JsonErrorField;

        public ErrorInfo() { }

        public ErrorInfo(ErrorCode code, string message, string errorField = "")
        {
            Code = code;
            Message = message;
            JsonErrorField = errorField;
        }

        public string LogDescription()
        {
            string log = string.Format("ErrorCode: {0}. Msg: {1}. ", Code, Message);
            if (!string.IsNullOrEmpty(JsonErrorField))
                log += string.Format("Error field: {0}", JsonErrorField);
            return log;
        }
    }
}
