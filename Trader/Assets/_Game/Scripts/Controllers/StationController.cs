﻿using UnityEngine;
using System.Collections;
using CodeControl;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

public class StationController : Controller<StationModel> {

    public SpriteRenderer background;
    internal GameManager game;
    internal SpriteRenderer sprite;
    internal float timeUpdate;
    internal LineRenderer line;
    internal float money
    {
        get
        {
            return model.money;
        }
    }
    protected override void OnInitialize()
    {
        sprite = GetComponent<SpriteRenderer>();
        line = GetComponent<LineRenderer>();
        name = model.name;
        timeUpdate = model.age.time + Date.Hour;
        sprite.color = model.color;

        background.color = model.backgroundColor;
        transform.position = model.position;
        game = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<GameManager>();

        model.moneyStats = new DataGraph("Money Over Time", "Time (hours)", "Money");
        model.moneyStats.data.Add("Money", new List<Stat>() { new Stat(model.age.hour, model.money) });
        model.moneyStats.data.Add("Money Change", new List<Stat>());

        if (model.parentStationLocation != Vector3.zero)
        {
            GetComponent<LineRenderer>().SetPositions(new Vector3[] { transform.position, model.parentStationLocation });
        }
    }
    protected override void OnModelChanged()
    {
        line.SetPositions(new Vector3[] { transform.position, model.lineTarget });
        model.lineColor.a = 1.1f - Mathf.Pow(.1f, 100 / (model.lineTarget - transform.position).magnitude);
        line.startColor = model.lineColor;
        line.endColor = model.lineColor;
        line.startWidth = model.lineColor.a;
        line.endWidth = model.lineColor.a;
    }
    // Update is called once per frame
    void Update() {

        int factoryStatus = model.factory.UpdateProduction(Time.deltaTime);
        
        transform.localScale = Vector3.one * 5 * (model.money / 1000000f + .5f);
        foreach (Items item in model.factory.outputItems)
        {
            if (item.selfProducing && factoryStatus > 0)
            {
                model.money += item.basePrice * factoryStatus * item.ratio;
            }
        }

        model.age.AddTime(Time.deltaTime);
        if (timeUpdate < model.age.time)
        {
            timeUpdate = model.age.time + Date.Hour;

            //Money Evaluation
            model.money -= model.runningCost;

            foreach (CreatureModel worker in model.workers)
            {
                worker.money += 10;
                model.money -= 10;
            }

            if (model.captain.Model != model.owner.Model)
            {
                model.captain.Model.money += 15;
                model.money -= 15;
            }

            float moneyEarned = model.money - model.moneyStats.data["Money"][model.moneyStats.data["Money"].Count - 1].y;

            if (moneyEarned > 0)
            {
                model.owner.Model.money += moneyEarned * .25f;
                model.money -= moneyEarned * .25f;

                model.captain.Model.money += moneyEarned * .1f;
                model.money -= moneyEarned * .1f;
            }
            moneyEarned = model.money - model.moneyStats.data["Money"][model.moneyStats.data["Money"].Count - 1].y;
            model.moneyChange = moneyEarned;
            model.moneyStats.data["Money Change"].Add(new Stat(model.age.time, moneyEarned));
            model.moneyStats.data["Money"].Add(new Stat(model.age.time, model.money));


        }

        if (money < 0)
        {
            foreach (ShipModel ship in model.incomingShips)
            {
                ship.NotifyChange();
            }
            print(model.name + " Died");
            model.Delete();
        }
        else if (money > 5000000)
        {
            int index = UnityEngine.Random.Range(0, 10);
            Vector3 randomLocation = new Vector3(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20));
            StationModel station = StationCreator.CreateStation((FactoryType)index, transform.position + randomLocation, transform);
            game.data.stations.Add(station);
            game.UpdateCreatures(station);
            model.money -= 1750000;
            model.owner.Model.money += 1000000;
            print(name + " Bought " + (FactoryType)index);
        }
    }

    public string GetInfo()
    {
        string info = "";
        info += "Factory Name: <color=" + ColorTypeConverter.ToRGBHex(model.color) + ">" + model.factory.name + "</color>\nMoney: " + model.money + "\n";
        info += string.Format("Owner: {0}\nCaptain: {1}\n Number Workers: {2}/{3}\n", model.owner.Model.name, model.captain.Model.name, model.workers.Count, model.workerCapacity);
        info += "Progress: " + (model.factory.productionTime / model.factory.unitTime).ToString("0.00") + " - " + model.factory.unitTime + "\n\n";

        foreach (Items item in model.factory.inputItems)
        {
            if (item.selfProducing == false)
                info += "Input " + item.coloredName + ": " + item.amount + " (" + (item.pendingAmount - item.amount) + ")/" + item.maxAmount + "| Price: " + item.price.ToString("0.00") + " - " +item.basePrice +  "\n";
            else
                info += "Input " + item.coloredName + ": ---\n";
        }
        foreach (Items item in model.factory.outputItems)
        {
            info += "Output " + item.coloredName + ": " + item.amount + "/" + item.maxAmount + "| Price: " + item.price.ToString("0.00") + " - " + item.basePrice + "\n";
        }

        List<ShipModel> sortedShips = new List<ShipModel>();
        foreach (ShipModel shipModel in model.incomingShips)
        {
            sortedShips.Add(shipModel);
        }
        sortedShips.Sort(delegate (ShipModel c1, ShipModel c2) { return c2.item.amount.CompareTo(c1.item.amount); });

        for (int i = 0; i < sortedShips.Count; i++)
        {
            info += string.Format("\n{0}. {1} - {2} | {3} - {4}", i + 1, sortedShips[i].name, sortedShips[i].money.ToString("0.00"), sortedShips[i].item.coloredName, sortedShips[i].item.amount);
        }
        info += "\n\n";
        List<Stat> moneyStats = new List<Stat>();
        moneyStats.AddRange(model.moneyStats.data["Money"]);
        moneyStats.Reverse();
        foreach (Stat stat in moneyStats)
        {
            if (stat.x > (model.age.time - Date.Day))
                info += string.Format("\n{0}. {1}", (stat.x / Date.Hour).ToString("0"), stat.y.ToString("0.00"));
        }

        return info;
    }

    internal Items Buy(string itemName, int itemAmount, ShipModel buyer = null)
    {
        foreach (Items item in model.factory.outputItems)
        {
            if (item.name == itemName)
            {
                if (itemAmount > item.amount)
                {
                    itemAmount = item.amount;
                }
                Items soldItem = new Items(itemName, itemAmount);
                soldItem.price = item.price;
                soldItem.totalPrice = soldItem.price * soldItem.amount;

                model.money += soldItem.totalPrice;
                item.amount -= itemAmount;
                model.factory.SetPrices();

                if (item.name == "Ship" && itemAmount > 0)
                {
                    item.amount += itemAmount;
                    if (buyer != null )
                    {
                        ShipModel ship = ShipCreator.CreateShip(buyer.name + "." + buyer.index, transform.position);
                        game.data.ships.Add(ship);
                        game.UpdateCreatures(ship);
                        buyer.index++;
                        buyer.item.totalPrice = soldItem.totalPrice;
                    }
                    else game.data.ships.Add(ShipCreator.CreateShip("Freight Ship", transform.position));
                }

                return soldItem;
            }

        }

        return null;
    }

    internal float Sell(Items sellItem)
    {
        foreach (Items item in model.factory.inputItems)
        {
            if (item.name == sellItem.name)
            {
                item.pendingAmount += sellItem.amount;

                model.money -= item.price * sellItem.amount;
                float price = item.price * sellItem.amount;
                model.factory.SetPrices();
                return price;
            }
        }

        return -1;
    }
    internal void SellComplete(Items sellItem)
    {
        foreach (Items item in model.factory.inputItems)
        {
            if (item.name == sellItem.name)
            {
                item.amount += sellItem.amount;
            }
        }
    }
    internal void SellIncomplete(Items sellItem)
    {
        foreach (Items item in model.factory.inputItems)
        {
            if (item.name == sellItem.name)
            {
                item.pendingAmount -= sellItem.amount;
                model.money += item.price * sellItem.amount;
                model.factory.SetPrices();
            }
        }
    }

    internal Items[] GetInputItems()
    {
        return model.factory.inputItems;
    }

    internal Items[] GetOutputItems()
    {
        return model.factory.outputItems;
    }
    internal CreatureModel GetOwner()
    {
        return model.owner.Model;
    }
    internal void AddIncoming(ShipModel ship)
    {
        model.incomingShips.Add(ship);
    }
    internal void RemoveIncoming(ShipModel ship)
    {
        model.incomingShips.Remove(ship);
    }
}