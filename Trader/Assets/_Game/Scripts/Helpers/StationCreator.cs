﻿using CodeControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationCreator {

    public static StationModel CreateStation(FactoryType name, Vector3 location, Transform parent = null)
    {
        StationModel model = new StationModel();
        NameGen names = new NameGen();
        if (parent == null)
        {
            model.owner = new ModelRef<CreatureModel>( new CreatureModel(names.GenerateMaleFirstName() + " " + names.GenerateRegionName(), 1000000));
            model.captain = new ModelRef<CreatureModel>(model.owner.Model);   
        }
        else
        {
            StationController parentStation = parent.GetComponent<StationController>();
            model.owner = new ModelRef<CreatureModel>(parentStation.GetOwner());
            model.parentStationLocation = parent.position;
        }

        model.captain = new ModelRef<CreatureModel>(new CreatureModel(names.GenerateMaleFirstName() + " " + names.GenerateRegionName()));
        model.owner.Model.stations.Add(model);
        model.workers = new ModelRefs<CreatureModel>();
        model.workers.Add(new CreatureModel(names.GenerateMaleFirstName() + " " + names.GenerateRegionName()));
        model.workerCapacity = 50;

        model.name = name + " Station";
        model.position = location;
        model.capacity = 10000;
        System.Random rand = new System.Random(model.name.GetHashCode());
        float a = rand.Next(1000)/1000f;
        float b = rand.Next(1000)/1000f;
        float c = rand.Next(1000)/1000f;
        model.color = new Color(a,b,c);
        a = rand.Next(1000) / 1000f;
        b = rand.Next(1000) / 1000f;
        c = rand.Next(1000) / 1000f;
        model.backgroundColor = new Color(a, b, c);

        model.factory = new Factory(name);

        foreach (Items item in model.factory.inputItems)
        {
            item.pendingAmount = item.amount;
            
        }
        foreach (Items item in model.factory.outputItems)
        {
            item.pendingAmount = item.amount;
        }

        //Money Setup
        model.money = 1000000f;
        model.owner.Model.money -= 1000000;

        model.moneyStats.data.Add("Money", new List<Stat>() { new Stat(model.age.hour, model.money) });
        model.moneyStats.data.Add("Money Change", new List<Stat>());

        StationController station = Controller.Instantiate<StationController>("station", model);

        return model;
    }
}