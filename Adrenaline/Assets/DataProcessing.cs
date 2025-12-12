using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DataProcessing
{
    public int NumberOfFalls;
    public int TimesShieldUsed;
    public float TimeSpentAFK;
    public float Timespent3rdPerson;
    public float Timespent1stPerson;

    public DataProcessing(int NumberOfFalls, int TimesShieldUsed, float timeSpentAFK, float timespent3rdPerson, float timespent1stPerson)
    {
        this.NumberOfFalls = NumberOfFalls;
        this.TimesShieldUsed = TimesShieldUsed;
        this.TimeSpentAFK = timeSpentAFK;
        this.Timespent3rdPerson = timespent3rdPerson;
        this.Timespent1stPerson = timespent1stPerson;
    }
}
