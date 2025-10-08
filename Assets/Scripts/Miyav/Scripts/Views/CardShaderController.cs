using UnityEngine;
using UnityEngine.UI;

namespace DorkyProductions.Views
{

    public class CardShaderController : MonoBehaviour
    {
        private Image _image;
        private Material _mat;
        
        // Supported Editions: "REGULAR", "POLYCHROME", "FOIL", "NEGATIVE"
        void Start()
        {
            ActivateEdition("REGULAR");
        }

        public void ShowSpecialCardVisuals()
        {
            ActivateEdition("POLYCHROME");
        }

        public void ShowRegularCardVisuals()
        {
            ActivateEdition("REGULAR");
        }

        private void ActivateEdition(string editionKeyword)
        {
            _image = GetComponent<Image>();
            _mat = new Material(_image.material);
            _image.material = _mat;
            for (int i = 0; i < _image.material.enabledKeywords.Length; i++)
            {
                _image.material.DisableKeyword(_image.material.enabledKeywords[i]);
                _image.material.EnableKeyword("_EDITION_" + editionKeyword);
            }
            SetCurrentRotation(transform.parent.localRotation);
        }

        // void Update()
        // {
            // SetCurrentRotation(transform.parent.localRotation);
        // }

        private void SetCurrentRotation(Quaternion currentRotation)
        {
            Vector3 eulerAngles = currentRotation.eulerAngles;

            float xAngle = eulerAngles.x;
            float yAngle = eulerAngles.y;

            xAngle = ClampAngle(xAngle, -90f, 90f);
            yAngle = ClampAngle(yAngle, -90f, 90);

            _mat.SetVector("_Rotation",
                new Vector2(ExtensionMethods.Remap(xAngle, -20, 20, -.5f, .5f),
                    ExtensionMethods.Remap(yAngle, -20, 20, -.5f, .5f)));
        }

        float ClampAngle(float angle, float min, float max)
        {
            if (angle < -180f)
                angle += 360f;
            if (angle > 180f)
                angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}