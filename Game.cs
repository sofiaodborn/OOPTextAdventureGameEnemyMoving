using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Schema;
using System.Diagnostics.Tracing;
using System.Linq;

public class Scoreboard
{
    public uint Score { get; set; }
}

public class Room
{
    public bool FinalRoom { get; set; }
    public string RoomDescription { get; set; }
    public List<Item> Items { get; set; }

    // List with Enemy Objects 
    public List<Enemy> Enemies { get; set; }

    // collection of keys and values
    public Dictionary<string, Room> Transitions { get; set; }

    public void PrintRoom()
    {
        Console.WriteLine(this.RoomDescription);
        if (Items.Count > 0)
        {
            foreach (Item item in Items)
            {
                Console.WriteLine(item);
            }
        }
        if (Enemies.Count > 0)
        {
            foreach (Enemy enemy in Enemies)
            {
                if (enemy.IsDestroyed)
                {
                    Console.WriteLine($"{enemy.Name} lies destroyed here.");
                }
                else 
                {
                    Console.WriteLine($"{enemy.Name} is in the room!");
                }
                
            }
            
        }
    }
}

public class Item
{
    public string Name { get; set; }
    public string Description { get; set; }
    public uint PointValue { get; set; }
    public override string ToString()
    {
        return $"{Name}: ({PointValue}) {Description}";
    }
}

public class Backpack
{
    public List<Item> Items { get; set; }
}

public class Weapon : Item, IWeapon
{
    public void Attack(IAttackable attackable)
    {
        attackable.IsDestroyed = true;      
    }
}

public class Harpsichord : Item, IAttackable
{
    public bool IsDestroyed { get; set; }
}

public class Enemy : IAttackable
{
    Random random = new Random();
    public string Name { get; set; }
    public bool IsDestroyed { get; set; }

    public Room SwitchRoom (Room room)
    {
        // random gets number of key/value pairs contained in enemyRoom.Transitions
        // Next picks a number in the interval
        int index = random.Next(room.Transitions.Count);
        // ElementAt returns value at specified index
        Room newRoom = room.Transitions.Values.ElementAt(index);

        // prvents the enemy from ending up in a final room
        while (newRoom.FinalRoom)
        {
            index = random.Next(room.Transitions.Count);
            newRoom = room.Transitions.Values.ElementAt(index);
        }
        return newRoom;
    }
}

public interface IWeapon
{
    void Attack(IAttackable attackable);
}

public interface IAttackable
{
    bool IsDestroyed { get; set; }
}


public class Game
{
    protected Scoreboard scoreboard = new Scoreboard();
    protected Room currentRoom;
    protected Room enemyRoom;
    protected Backpack backpack;
    protected string introduction = "";
    protected int counter;
    protected int index;

    public void StartGame()
    {
        Console.WriteLine(introduction);
        InputLoop();
        PrintScore();
    }

    public void InputLoop()
    {
        while (true)
        {
            currentRoom.PrintRoom();
            string input = Console.ReadLine();
            if (input == "quit")
            {
                break;
            }

            if (input.StartsWith("pick up "))
            {
                string item = input.Split("up ")[1];
                if (currentRoom.Items.Exists(x => x.Name == item))
                {
                    Item PUitem = currentRoom.Items.Find(x => x.Name.Contains(item));
                    backpack.Items.Add(PUitem);
                    currentRoom.Items.Remove(PUitem);
                    scoreboard.Score += PUitem.PointValue;
                }
                else
                {
                    Console.WriteLine("The item you tried to pick up does not exist. Try again.");
                }
            }

            if (input.StartsWith("attack")) 
            {
                var regex = new Regex(@"attack ([\w\s]+) with ([\w\s]+)", RegexOptions.IgnoreCase);
                var match = regex.Match(input);
                if (match.Success)
                {
                    string attackableName = match.Groups[1].Value;
                    string weaponName = match.Groups[2].Value;

                    // find matching weapon
                    // null if weapon is not an IWeapon
                    // null if attackable is not an IAttackable
                    IWeapon weapon = FindWeapon(weaponName);
                    IAttackable attackable = FindAttackable(attackableName);

                    if (weapon != null && attackable != null)
                    {
                        if (attackable is Harpsichord)
                        {
                            // gold is added to Item list in Room
                            currentRoom.Items.Add(new Item { Name = "gold", Description = "Shiny gold coins.", PointValue = 100 });
                            // harpsichord is removed from Item list in Room
                            currentRoom.Items.Remove(attackable as Harpsichord);
                        }
                        //IsDestroyed for object becomes true
                        weapon.Attack(attackable);
                    }
                }
            }

            else if (input.StartsWith("drop "))
            {
                string item = input.Split("drop ")[1];
                if (backpack.Items.Exists(x => x.Name == item))
                {
                    Item Ditem = backpack.Items.Find(x => x.Name.Contains(item));
                    backpack.Items.Remove(Ditem);
                    currentRoom.Items.Add(Ditem);
                    scoreboard.Score -= Ditem.PointValue;
                }
                else
                {
                    Console.WriteLine("The item you tried to drop does not exist. Try again.");
                }
                continue;
            }

            else if (input.StartsWith("describe "))
            {

                string item = input.Split("describe ")[1];
                if (currentRoom.Items.Exists(x => x.Name == item))
                {
                    Console.WriteLine(currentRoom.Items.Find(x => x.Name == item));
                }
                else if (backpack.Items.Exists(x => x.Name == item))
                {
                    Console.WriteLine(backpack.Items.Find(x => x.Name == item));
                }
                else
                {
                    Console.WriteLine("The item is not available.");
                }
                continue;
            }

            // TryGetValue gets the value associated with the specified key.
            if (currentRoom.Transitions.TryGetValue(input, out Room nextRoom))
            {
                if (currentRoom.Enemies.Exists(enemy => (enemy.IsDestroyed == false)))
                {
                    scoreboard.Score = 0;
                    break;
                }
                else
                {
                    currentRoom = nextRoom;
                    counter++;

                    // if counter has an even number (counter is even every other time a player switches rooms)
                    // & if enemy is still alive
                    if (counter%2 == 0 && enemyRoom.Enemies.Exists(enemy => enemy.IsDestroyed == false))
                    {
                        Enemy enemy = enemyRoom.Enemies.Find(enemy => enemy.IsDestroyed == false);
                        enemyRoom.Enemies.Remove(enemy);

                        Room newEnemyRoom = enemy.SwitchRoom(enemyRoom);

                        enemyRoom = newEnemyRoom;
                        enemyRoom.Enemies.Add(enemy);
                    }
                }
            }

            if (currentRoom.FinalRoom)
            {
                currentRoom.PrintRoom();
                break;
            }
        }
    }


    public IWeapon FindWeapon(string name)
    {
        Item foundItem = backpack.Items.Find(item => item.Name.ToLower() == name.ToLower());
        
        IWeapon weapon = foundItem as IWeapon; //this cast returns null if foundItem isn't an IWeapon
        if (weapon == null)
        {
            Console.WriteLine($"Couldn't find a weapon named {name}");
        }
        return weapon;
    }

    public IAttackable FindAttackable(string name)
    {
        Enemy enemy = currentRoom.Enemies.Find(enemy => enemy.Name.ToLower() == name.ToLower());

        IAttackable attackableItem = currentRoom.Items.Find(item => item.Name.ToLower() == name.ToLower()) as IAttackable;

        if (enemy == null && attackableItem == null)
        {
            Console.WriteLine($"Couldn't find an attackable named {name}");
        }

        // the following "??" is called a "null coalesce" operator.
        // If the left side is null, it will return the right side. Otherwise it returns the left side.
        // (if both are null, returns null)
        return enemy ?? attackableItem;
    }

    public void PrintScore()
    {
        Console.WriteLine($"Your score was: {scoreboard.Score}");
    }
}


