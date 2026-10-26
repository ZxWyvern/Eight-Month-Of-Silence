using UnityEngine;
using UnityEngine.InputSystem;

namespace PSXShaderKit
{
    public class PSXExample_CameraMovement : MonoBehaviour
    {
        public float speed = 0.5f;

        // Update is called once per frame
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return;
            float vertical = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            float horizontal = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            transform.position += new Vector3(transform.forward.x, 0, transform.forward.z) * vertical * speed * Time.deltaTime;
            transform.position += new Vector3(transform.right.x, 0, transform.right.z) * horizontal * speed * Time.deltaTime;
        }
    }
}
