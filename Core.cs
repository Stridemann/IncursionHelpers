using System;
using System.Collections.Generic;
using System.IO;
using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using SharpDX;

namespace IncursionHelpers
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        private readonly Dictionary<string, Room> _rooms = new Dictionary<string, Room>();
        private bool _wasOpened;
        private Room _room1;
        private Room _room2;

        public override bool Initialise()
        {
            Load();
            return base.Initialise();
        }


        public override void Render()
        {
            var incursionWindow = GameController.Game.IngameState.IngameUi.IncursionWindow;

            if (incursionWindow.IsVisibleLocal)
            {
                if (!_wasOpened)
                {
                    _wasOpened = true;
                    ParseRooms(incursionWindow, out var roomType1, out var roomType2);
                    UpdateRewards(roomType1, roomType2);
                }

                DrawRewards(incursionWindow);
            }
            else
            {
                _wasOpened = false;
            }
        }

        private void DrawRewards(IncursionWindow incursionWindow)
        {
            var topRight = incursionWindow.GetClientRectCache.TopRight;
            if (_room1 != null)
            {
                var room1DrawPos = topRight;
                room1DrawPos.X -= 80;
                room1DrawPos.Y += 150;
                foreach (var room in _room1.AllRooms)
                {
                    var textSize = Graphics.DrawText(room.Outcome, room1DrawPos, room.Color);
                    var border = 2;
                    var rectangleF = new RectangleF(
                        room1DrawPos.X - border * 2,
                        room1DrawPos.Y - border,
                        textSize.X + border * 4,
                        textSize.Y + border * 2);
                    Graphics.DrawBox(rectangleF, Color.Black);

                    if (room == _room1)
                    {
                        Graphics.DrawFrame(rectangleF, Color.White, 2);
                    }

                    room1DrawPos.Y += 20;
                }
            }

            if (_room2 != null)
            {
                var room1DrawPos = topRight;
                room1DrawPos.X -= 350;
                room1DrawPos.Y += 50;

                foreach (var room in _room2.AllRooms)
                {
                    var textSize = Graphics.DrawText(room.Outcome, room1DrawPos, room.Color, FontAlign.Right);
                    var border = 2;
                    var rectangleF = new RectangleF(
                        room1DrawPos.X - textSize.X - border * 2,
                        room1DrawPos.Y - border,
                        textSize.X + border * 4,
                        textSize.Y + border * 2);
                    Graphics.DrawBox(rectangleF, Color.Black);

                    if (room == _room2)
                    {
                        Graphics.DrawFrame(rectangleF, Color.White, 2);
                    }

                    room1DrawPos.Y += 20;
                }
            }
        }

        private void UpdateRewards(string roomType1, string roomType2)
        {
            _rooms.TryGetValue(roomType1, out _room1);
            _rooms.TryGetValue(roomType2, out _room2);
        }

        private void ParseRooms(IncursionWindow incursionWindow, out string roomType1, out string roomType2)
        {
            const string trimWord = " to ";

            roomType1 = incursionWindow.Reward2;
            roomType2 = incursionWindow.Reward1;

            var splitIndex1 = roomType1.LastIndexOf(trimWord);
            var splitIndex2 = roomType2.LastIndexOf(trimWord);

            if (splitIndex1 != -1)
            {
                roomType1 = roomType1.Substring(splitIndex1 + trimWord.Length);
                roomType1 = roomType1.Substring(0, roomType1.Length - 2);
            }
            else
                roomType1 = $"Error: Cannot parse room type from: {roomType1}";

            if (splitIndex2 != -1)
            {
                roomType2 = roomType2.Substring(splitIndex2 + trimWord.Length);
                roomType2 = roomType2.Substring(0, roomType2.Length - 2);
            }
            else
                roomType2 = $"Error: Cannot parse room type from: {roomType2}";
        }

        #region Config loader

        private void Load()
        {
            var lines = File.ReadAllLines(Path.Combine(DirectoryFullName, "IncursionConfig.txt"));

            var parse = false;
            for (var i = 0; i < lines.Length - 1; i++)
            {
                var line = lines[i];

                if (!parse)
                {
                    if (line.StartsWith("===ParseStartLine:"))
                        parse = true;
                }
                else
                {
                    var roomNames = line.Split('|');
                    var roomOutcomes = lines[i + 1].Split('|');

                    if (roomNames.Length != 3)
                    {
                        LogError($"Incursion helpers: expecting 3 room names in line {i}", 5);
                        i++;
                        continue;
                    }

                    if (roomOutcomes.Length != 3)
                    {
                        LogError($"Incursion helpers: expecting 3 room outcomes in line {i}", 5);
                        i++;
                        continue;
                    }

                    var allRooms = new List<Room>();

                    for (var j = 0; j < 3; j++)
                    {
                        var roomName = roomNames[j];
                        var roomOutcome = roomOutcomes[j];

                        var color = GetColor(ref roomOutcome);

                        if (roomName[roomName.Length - 2] == ':') //colors in room name was useless
                            roomName = roomName.Substring(0, roomName.Length - 2);

                        var roomValue = new Room(roomName, roomOutcome, color, allRooms);
                        allRooms.Add(roomValue);

                        if (!_rooms.ContainsKey(roomName))
                            _rooms.Add(roomName, roomValue);
                        else
                            LogError($"Incursion helpers: Room {roomName} is already registered! Line: {j}", 5);
                    }

                    i++;
                }
            }
        }

        private Color GetColor(ref string name)
        {
            if (name[name.Length - 2] == ':')
            {
                var color = name[name.Length - 1];
                name = name.Substring(0, name.Length - 2);

                switch (color)
                {
                    case 'G': return Color.Green;
                    case 'Y': return Color.Yellow;
                }
            }

            return Color.Gray;
        }

        #endregion

        private class Room
        {
            public Room(string roomName, string outcome, Color color, List<Room> allRooms)
            {
                RoomName = roomName;
                Outcome = outcome;
                Color = color;
                AllRooms = allRooms;
            }

            public string RoomName { get; }
            public string Outcome { get; }
            public Color Color { get; }

            public List<Room> AllRooms { get; }
        }
    }
}