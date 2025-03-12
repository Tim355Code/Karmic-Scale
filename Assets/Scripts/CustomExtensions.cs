using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// Utility class imported from a different project, most stuff is unused
public static class VectorExtensions
{
    public static float InverseLerp(Vector3 from, Vector3 to, Vector3 value)
    {
        Vector3 fromTo = to - from;
        Vector3 fromValue = value - from;

        // Project the fromValue vector onto the fromTo vector to get the distance along the fromTo vector
        float dotProduct = Vector3.Dot(fromValue, fromTo);
        float magnitudeSquared = fromTo.sqrMagnitude;

        // Calculate the t value
        float t = dotProduct / magnitudeSquared;

        // Clamp t to the range [0, 1]
        t = Mathf.Clamp01(t);

        return t;
    }

    public static Vector2 AngleToVec2(float angle)
    {
        angle *= Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public static Vector3 BezierPoint(Vector3 anchor1, Vector3 anchor2, Vector3 control1, Vector3 control2, float t)
    {
        // Ensure t is clamped between 0 and 1
        t = Mathf.Clamp01(t);

        // Calculate the point using the cubic Bézier formula
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * oneMinusT * anchor1 +
            3f * oneMinusT * oneMinusT * t * control1 +
            3f * oneMinusT * t * t * control2 +
            t * t * t * anchor2;
    }

    public static float Vec2ToAngle(Vector2 vector)
    {
        return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
    }
}

public class Interpolator<T>
{
    public bool HasFinished;

    private float Duration;
    public float Timer;

    private T StartState;
    private T EndState;

    private float StartTime;

    public Interpolator(float duration, T startState, T endState)
    {
        HasFinished = false;

        Duration = duration;
        StartState = startState;
        EndState = endState;

        StartTime = Time.time;
    }

    public float TimeSinceEnd()
    {
        return Time.time - (StartTime + Duration);
    }

    public T Update(float deltaTime)
    {
        Timer += deltaTime;

        if (Timer >= Duration) HasFinished = true;

        return Lerp(StartState, EndState, Mathf.Clamp01(Timer / Duration));
    }

    private T Lerp(T start, T end, float t)
    {
        if (typeof(T) == typeof(Vector2))
        {
            return (T)(object)Vector2.Lerp((Vector2)(object)start, (Vector2)(object)end, t);
        }
        if (typeof(T) == typeof(Vector3))
        {
            return (T)(object)Vector3.Lerp((Vector3)(object)start, (Vector3)(object)end, t);
        }
        if (typeof(T) == typeof(float))
        {
            return (T)(object)Mathf.Lerp((float)(object)start, (float)(object)end, t);
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)Mathf.RoundToInt(Mathf.Lerp((int)(object)start, (int)(object)end, t));
        }
        if(typeof(T) == typeof(Color))
        {
            return (T)(object)Color.Lerp((Color)(object)start, (Color)(object)end, t);
        }

        throw new NotSupportedException($"Type {typeof(T)} is not supported by StateUpdater.");
    }
}

// Classes to abstract away basic aiming settings
[System.Serializable]
public struct AimSettings
{
    public float AimAngle;
    public float SpreadAngle;
    public bool GoOutwards;
}

public class Aimer
{
    private float CurrentAngle;
    private float DeltaAngle;

    public Aimer(AimSettings settings, int count)
    {
        if (count <= 1)
        {
            CurrentAngle = settings.AimAngle;
            DeltaAngle = 0;

            return;
        }

        CurrentAngle = settings.AimAngle + (settings.GoOutwards ? -settings.SpreadAngle : settings.SpreadAngle);
        DeltaAngle = settings.GoOutwards ? settings.SpreadAngle / count : -settings.SpreadAngle / count;
    }

    public float NextAngle()
    {
        var angle = CurrentAngle;
        CurrentAngle += DeltaAngle;

        return angle;
    }
}
