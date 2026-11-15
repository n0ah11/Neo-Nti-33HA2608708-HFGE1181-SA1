using System;
using Unity.Collections;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private Vector3 relativePosition;
    [SerializeField] private float baseZOffset;
    private SizeHandler sizeHandler;
    
    private void Awake()
    {
        sizeHandler = target.GetComponent<SizeHandler>();
    }

    private void Start()
    {
        AdjustPosition();
        
    }

    private void Update()
    {
        AdjustPosition();
    }

    private void AdjustPosition()
    {
        if (target == null || sizeHandler == null) return; // SA1: null guard
        float size = sizeHandler.GetSize();
        relativePosition = new Vector3(target.transform.position.x, target.transform.position.y, baseZOffset - (size -1f));
        transform.position = relativePosition;
    }
}
