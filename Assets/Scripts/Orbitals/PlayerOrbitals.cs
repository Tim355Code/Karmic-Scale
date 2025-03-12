using UnityEngine;
using System.Collections.Generic;

// Handles current orbitals and tries to evenly space them out as new ones are added
public class PlayerOrbitals : MonoBehaviour
{
    public static PlayerOrbitals Instance;

    private List<Orbital> CurrentOrbitals;

    void Awake()
    {
        Instance = this;
        CurrentOrbitals = new List<Orbital>();
    }

    public void SpawnOrbital(Orbital prefab)
    {
        var orbital = Instantiate(prefab, transform);
        orbital.transform.localPosition = new Vector3(0, -prefab.Radius);
        CurrentOrbitals.Add(orbital);

        var deltaAngle = CurrentOrbitals.Count > 1 ? Mathf.PI * 2 / CurrentOrbitals.Count : 0;
        for (int i = 0; i < CurrentOrbitals.Count; i++)
        {
            CurrentOrbitals[i].Rotation = deltaAngle * i;
        }
    }

    void FixedUpdate()
    {
        foreach (var orbital in CurrentOrbitals)
        {
            orbital.Rotation += orbital.RotationSpeed * Time.fixedDeltaTime / (2 * Mathf.PI);

            orbital.transform.localPosition = new Vector3(Mathf.Sin(orbital.Rotation) , - Mathf.Cos(orbital.Rotation), 0) * orbital.Radius;
        }
    }
}
