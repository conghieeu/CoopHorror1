using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NewBehaviourScript : MonoBehaviour
{
    [Networked]
    public int exampleNetworkedVariable { get; set; }
}
