using System.Collections;
using System.Collections.Generic;
using naviar.VPSService.JSONs;
using UnityEngine;

namespace naviar.VPSService
{
    public interface IRequestVPS
    {
        /// <summary>
        /// Set vps server url for sending requests
        /// </summary>
        void SetUrl(string url);
        /// <summary>
        /// Send requst: image and meta
        /// </summary>
        IEnumerator SendVpsRequest(Texture2D image, string meta, System.Action callback);
        /// <summary>
        /// Send requst: meta and mobileVPS result
        /// </summary>
        IEnumerator SendVpsRequest(byte[] embedding, string meta, System.Action callback);
        /// <summary>
        /// Send requst: image, meta and mobileVPS result
        /// </summary>
        IEnumerator SendVpsRequest(Texture2D image, byte[] embedding, string meta, System.Action callback);
        /// <summary>
        /// Get latest request location state
        /// </summary>
        LocationState GetLocationState();
    }
}