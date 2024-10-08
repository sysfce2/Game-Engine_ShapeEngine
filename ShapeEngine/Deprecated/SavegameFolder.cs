﻿using ShapeEngine.Lib;

namespace ShapeEngine.Deprecated
{
    public class SavegameFolder
    {
        public string Path { get; private set; }
        public SavegameFolder(params string[] folders)
        {
            Path = ShapeSavegame.CombinePath(folders);
        }
        public SavegameFolder(SavegameFolder root,  params string[] folders)
        {
            Path = root.Path + ShapeSavegame.CombinePath(folders);
        }

        public bool Save<T>(T data, string filename) { return ShapeSavegame.Save(data, Path, filename); }
        public T? Load<T>(string filename) { return ShapeSavegame.Load<T>(Path, filename); }
    }



    /*
    public class SavegameHandler
    {

        public static string APPLICATION_DATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public string MAIN_PATH = "";
        public string SAVEGAME_PATH = "";

        public SavegameHandler(string studioName, string gameName)
        {
            MAIN_PATH = APPLICATION_DATA_PATH + String.Format("\\{0}\\{1}", studioName, gameName);
            SAVEGAME_PATH = MAIN_PATH + "\\savegames";
        }

        //Make functions for relative paths based on how the class was constructed




        //functions for absolute paths
        public static bool Save<T>(T data, string path, string fileName)
        {
            if (data == null) return false;
            if (path.Length <= 0 || fileName.Length <= 0) return false;
            Directory.CreateDirectory(path);

            string data_string = JsonSerializer.Serialize(data);
            if (data_string.Length <= 0) return false;

            File.WriteAllText(path + "\\" + fileName, data_string);
            return true;
        }
        public static T? Load<T>(string path, string fileName)
        {
            path = path + "\\" + fileName;
            if (!File.Exists(path)) return default;

            var data_string = File.ReadAllText(path);


            return JsonSerializer.Deserialize<T>(data_string);
        }

    }
    */
}
