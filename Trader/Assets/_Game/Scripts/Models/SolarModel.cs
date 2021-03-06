﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeControl;

/// <summary>
/// Inherited by all center solar objects.
/// </summary>
public class SolarModel : Model {

    public string name;

    public bool isActive = false;
    /// <summary>
    /// position in galaxy in light years
    /// </summary>
    public Vector3 galacticPosition;
    public Color color;
    public SolarBody solar;
    public ModelRefs<StationModel> stations = new ModelRefs<StationModel>();
    public ModelRefs<ShipModel> ships = new ModelRefs<ShipModel>();
    public ModelRefs<SolarModel> nearStars = new ModelRefs<SolarModel>();
    public ModelRef<GovernmentModel> government = new ModelRef<GovernmentModel>();
    public float governmentInfluence;
    public bool isCapital;
    public int index;
    internal float localScale;

    public SolarModel() { }

    public SolarModel(string _name, int _index, Vector3 _position, Gradient sunSizeColor)
    {
        name = _name;
        galacticPosition = _position;
        index = _index;

        // Create star

        SolarSubType starSubType;
        double sunMass;
        double temp;
        float sunDensity;

        float star = Random.value;
        if (star < .5f)
        {
            starSubType = SolarSubType.MainSequence;
            sunMass = Random.Range(.1f, 60);
            temp = (42000f / 599f) * sunMass + (1755000 / 599);
            sunMass *= GameDataModel.sunMassMultiplication; // Sun Mass in solar masses
            sunDensity = Random.Range(.5f, 4) * Units.k;
        }
        else if (star < .75f)
        {
            starSubType = SolarSubType.GasGiant;
            sunMass = Random.Range(60f, 100);
            temp = Random.Range(4000, 45000);
            sunMass *= GameDataModel.sunMassMultiplication; // Sun Mass in solar masses
            sunDensity = Random.Range(.1f, 2) * Units.k;
        }
        else
        {
            starSubType = SolarSubType.WhiteDwarf;
            sunMass = Random.Range(.01f, 1);
            temp = Random.Range(6000, 30000);
            sunMass *= GameDataModel.sunMassMultiplication; // Sun Mass in solar masses
            sunDensity = Random.Range(10f, 2000) * Units.k;
        }
        //float sunColorValue = (float) (sunMass / 100 / GameDataModel.sunMassMultiplication);
        //Color color = sunSizeColor.Evaluate(sunColorValue);
        Color color = ColorTempToRGB.TempToRGB((float)temp);
        double sunRadius = Mathd.Pow(((sunMass / sunDensity) * 3) / (4 * Mathd.PI), 1 / 3d); // In meters
        var solarIndex = new List<int>();
        solarIndex.Add(_index);

        solar = new SolarBody(_name + " " + starSubType.ToString(), solarIndex, starSubType, sunMass , sunRadius, new Orbit(), color, temp);

        int numPlanets = (int)Random.Range(0, (float)Mathd.Pow(sunMass / GameDataModel.sunMassMultiplication, .25f) * 20);
        double sunSoi = solar.SOI(sunMass);

        for (int i = 0; i < numPlanets; i++)
        {
            double sma = Random.Range((float) (solar.bodyRadius) * 1.1f, (float) (sunSoi * .25));
            float lpe = Random.Range(0, 2 * Mathf.PI);

            float satType = Random.value;
            double planetMass;
            float planetDensity;
            SolarType planetType = SolarType.Planet;
            SolarSubType planetSubType = SolarSubType.Rocky;
            float ecc = Random.Range(0, .01f);

            if (satType < .4f) //Rock planets
            {
                
                planetMass = Random.Range(1f, 100f) * 1e+23;
                planetDensity = Random.Range(3.5f, 7) * Units.k;
            }
            else if (satType < .5f) //Gas Giants
            {
                planetDensity = Random.Range(.5f, 3) * Units.k;
                planetSubType = SolarSubType.GasGiant;
                planetMass = Random.Range(1, 400) * 1e+25;
            }
            else if (satType < .7f) //Dwarf Planets
            {
                planetType = SolarType.DwarfPlanet;
                planetDensity = Random.Range(3.5f, 7) * Units.k;
                planetMass = Random.Range(1, 40) * 1e+21;
                ecc = Random.Range(0.01f, .5f);
            }
            else //Meteors
            {
                planetDensity = Random.Range(3.5f, 7) * Units.k;
                planetType = SolarType.Comet;
                planetMass = Random.Range(1, 100) * 1e+13;
                ecc = Random.Range(0.5f, 1);
            }

            double planetRadius = Mathd.Pow((((planetMass) / planetDensity) * 3) / (4 * Mathd.PI), 1 / 3d); // In meters
            
            

            var planetIndex = new List<int>();
            planetIndex.Add(solarIndex[0]);
            planetIndex.Add(i);

            color = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

            solar.satelites.Add(new SolarBody(name + " " + planetType.ToString() + " " + (i + 1), planetIndex, planetType, planetSubType, planetMass, planetRadius, new Orbit(sma,ecc,lpe,0,lpe),color, solar));

            int numMoonRange = 0;
            float moonChance = 0;

            if (planetSubType == SolarSubType.GasGiant)
            {
                numMoonRange = 30;
                moonChance = 1;
            }
            else if (planetType == SolarType.DwarfPlanet)
            {
                numMoonRange = 2;
                moonChance = .1f;
            }
            else if (planetSubType == SolarSubType.Rocky)
            {
                numMoonRange = 5;
                moonChance = .5f;
            }
            int numMoon = Random.Range(0, numMoonRange);

            double soi = solar.satelites[i].SOI(solar.mass);

            if (Random.value < moonChance)
            {
                for (int m = 0; m < numMoon; m++)
                {
                    double moonMass = Random.Range(.0001f, 50) * 1e+22;

                    sma = Random.Range((float)solar.satelites[i].bodyRadius * 1.2f, (float)(soi * .75f));
                    lpe = Random.Range(0, 2 * Mathf.PI);
                    color = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
                    float moonDensity = Random.Range(3.5f, 7) * Units.k;
                    double moonRadius = Mathd.Pow((((moonMass) / moonDensity) * 3) / (4 * Mathd.PI), 1 / 3d); // In meters
                    SolarType moonType = SolarType.Moon;
                    SolarSubType moonSubType = SolarSubType.Rocky;
                    ecc = Random.Range(0, .02f);
                    if (moonMass < 1e+16)
                    {
                        moonType = SolarType.Asteroid;
                        ecc = Random.Range(0.02f, .5f);
                    }

                    var moonIndex = new List<int>();
                    moonIndex.AddRange(planetIndex);
                    moonIndex.Add(m);

                    solar.satelites[i].satelites.Add(new SolarBody(name + "Moon " + (i + 1), moonIndex, moonType, moonSubType, moonMass, moonRadius, new Orbit(sma, ecc, lpe, 0, lpe), color, solar));
                }
            }
            
        }
    }
}
