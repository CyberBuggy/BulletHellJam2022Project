using UnityEngine;

namespace Utils
{
    public class ConstantRotate : MonoBehaviour
    {
        [SerializeField] private Vector3 speed;
    
    
        // Update is called once per frame
        void Update()
        {
            transform.Rotate(speed * Time.smoothDeltaTime);
        }
    }
}
