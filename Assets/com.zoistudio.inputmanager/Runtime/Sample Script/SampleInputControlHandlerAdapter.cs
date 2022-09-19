using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZoiStudio.InputManager {

    public class SampleInputControlHandlerAdapter : MonoBehaviour {

        public string groupToActivate;

        void Start() {
            InputControlHandler<TouchData>.TransferControl(groupToActivate);
        }

    }
}