using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public GameObject UITextInfo;
    private TabletThalamusConnector _thalamusConnector;


    // Start is called before the first frame update
    void Start()
    {
        _thalamusConnector = new TabletThalamusConnector(7010);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
