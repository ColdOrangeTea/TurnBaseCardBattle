using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene002_PlanetRotation : MonoBehaviour
{
    public float rotationSpeed = 50f; // ±ÛÂà³t«×
    public Vector3 rotationDirection = Vector3.up; 

    void Update()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime;

        transform.Rotate(rotationDirection * rotationAmount, Space.Self);
    }
}
