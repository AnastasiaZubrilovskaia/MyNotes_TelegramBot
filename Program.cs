using System;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using static System.IO.File;
using SystemFile = System.IO.File;

namespace TelegramBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var client = new TelegramBotClient("8116181148:AAHPZrYOugr6-TfNmGl71lbOdQoiSVgCZ28");
            client.StartReceiving(UpdateHandler.Update, UpdateHandler.Error);

            Console.ReadLine();
        }
        
    }

    internal class UpdateHandler
    {
        private static NoteManager noteManager;
        private static Dictionary<long, bool> waitingForNote = new Dictionary<long, bool>();
        private static Dictionary<long, bool> waitingForDeleteNote = new Dictionary<long, bool>();


        public static async Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            try
            {
                var replyKeyboardMenu = new ReplyKeyboardMarkup(
                        new List<KeyboardButton[]>()
                        {
                        new KeyboardButton[]
                        {
                            new KeyboardButton("Создать заметку"),
                            new KeyboardButton("Список заметок"),
                        },
                        new KeyboardButton[]
                        {
                            new KeyboardButton("Помощь")
                        },

                        })
                {
                    ResizeKeyboard = true,
                };

                var message = update.Message;
                var user = message.From;
                noteManager = new NoteManager(user.Id);

                // Выводим на экран то, что пишут нашему боту, а также  информацию об отправителе
                Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");
                if (message.Text.ToLower().Contains("привет"))
                {
                    await client.SendMessage(message.Chat.Id, $"Hello!");
                    return;
                }

                if (message.Text == "/start")
                {
                    await client.SendMessage(message.Chat.Id, $"Hello! Выберите команду: {Environment.NewLine}/help {Environment.NewLine}/menu {Environment.NewLine}");
                    return;
                }

                if (message.Text == "/menu" || message.Text == "Назад в меню")
                {

                    await client.SendMessage(message.Chat.Id, "Основное меню", replyMarkup: replyKeyboardMenu);
                    return;

                }

                if (message.Text == "Создать заметку" || message.Text == "/addNote")
                {
                    var replyKeyboardCreateNote = new ReplyKeyboardMarkup(
                        new KeyboardButton("Отменить создание заметки"))
                    {
                        ResizeKeyboard = true,
                    };
                    await client.SendMessage(message.Chat.Id, "Введите текст заметки:", replyMarkup: replyKeyboardCreateNote);
                    waitingForNote[message.Chat.Id] = true;
                    return;
                }
                if (message.Text == "/cancelAddNote" || message.Text == "Отменить создание заметки")
                {
                    if (waitingForNote.ContainsKey(message.Chat.Id))
                    {
                        waitingForNote.Remove(message.Chat.Id); // убираем пользователя из списка ожидания
                        await client.SendMessage(message.Chat.Id, "Создание заметки отменено", replyMarkup: replyKeyboardMenu);
                    }
                    return;
                }
                else if (waitingForNote.ContainsKey(message.Chat.Id))
                {
                    // Обработка текста заметки
                    string noteText = message.Text;
                    waitingForNote.Remove(message.Chat.Id); // убираем пользователя из списка ожидания
                    await client.SendMessage(message.Chat.Id, $"Заметка создана: {noteText}");
                    noteManager.AddNote(noteText);
                    await client.SendMessage(message.Chat.Id, "Заметка добавлена!", replyMarkup: replyKeyboardMenu);
                    return;
                }

                if (message.Text == "Удалить заметку" || message.Text == "/deleteNote")
                {
                    var replyKeyboardDeleteNote = new ReplyKeyboardMarkup(
                            new KeyboardButton("Отменить удаление заметки"))
                    {
                        ResizeKeyboard = true,
                    };

                    await client.SendMessage(message.Chat.Id, "Введите номер заметки, которую хотите удалить", replyMarkup: replyKeyboardDeleteNote);
                    waitingForDeleteNote[message.Chat.Id] = true;

                    return;
                }
                if (message.Text == "/cancelDeleteNote" || message.Text == "Отменить удаление заметки")
                {
                    if (waitingForDeleteNote.ContainsKey(message.Chat.Id))
                    {
                        waitingForDeleteNote.Remove(message.Chat.Id); // убираем пользователя из списка ожидания
                        await client.SendMessage(message.Chat.Id, "Удаление заметки отменено", replyMarkup: replyKeyboardMenu);
                    }
                    return;
                }
                else if (waitingForDeleteNote.ContainsKey(message.Chat.Id))
                {
                    // Обработка текста 
                    string idStr = message.Text;
                    waitingForDeleteNote.Remove(message.Chat.Id); // убираем пользователя из списка ожидания
                    if (int.TryParse(idStr, out int id))
                    {
                        id--; 
                        if (id < 0 || id >= noteManager.GetAllNotes().Count) 
                        {
                            await client.SendMessage(message.Chat.Id, "Неверный идентификатор. Заметки под таким номером не существует", replyMarkup: replyKeyboardMenu);
                        }
                        else
                        {
                            noteManager.DeleteNote(id);
                            await client.SendMessage(message.Chat.Id, "Заметка успешно удалена!", replyMarkup: replyKeyboardMenu);
                        }
                    }
                    else
                    {
                        await client.SendMessage(message.Chat.Id, "Некорректный идентификатор заметки", replyMarkup: replyKeyboardMenu);
                    }

                    return;
                }

                if (message.Text == "Список заметок" || message.Text == "/listNotes")
                {
                    var replyKeyboardListNote = new ReplyKeyboardMarkup(
                        new KeyboardButton("Удалить заметку"), new KeyboardButton("Назад в меню"))
                    {
                        ResizeKeyboard = true,
                    };
                    var notes = noteManager.GetAllNotes();
                    if (!notes.Any())
                    {
                        await client.SendMessage(message.Chat.Id, "У Вас нет заметок.");
                    }
                    else
                    {
                        var numberedNotes = notes.Select((note, index) => $"{index + 1})" +
                        $" {note}");
                        string messageContent = $"Ваши заметки:{Environment.NewLine}" + string.Join($"{Environment.NewLine}", numberedNotes);
                        await client.SendMessage(message.Chat.Id, messageContent, replyMarkup: replyKeyboardListNote);
                    }
                    return;
                }


                if (message.Text == "Помощь" || message.Text == "/help")
                {
                    await client.SendMessage(message.Chat.Id,
                        $"Я Ваш бот-хранилище заметок.{Environment.NewLine} Вот список моих возможностей:{Environment.NewLine}" +
                        $"- Вы можете создавать заметки. Для этого в меню кнопок выберите \"Создать заметку\" {Environment.NewLine}" +
                        $"- Вы можете удалять свои заметки. Для этого в меню кнопок выберите \"Удалить заметку\"{Environment.NewLine}{Environment.NewLine}" +
                        $"А вот полный список моих команд:{Environment.NewLine}" +
                        $"/start - начать использовать бота {Environment.NewLine}" +
                        $"/menu - открыть главное меню {Environment.NewLine}" +
                        $"/help - открыть справку {Environment.NewLine}" +
                        $"/addNote - добавить заметку {Environment.NewLine}" +
                        $"/listNotes - показать список заметок {Environment.NewLine}" +
                        $"/deleteNote - удалить заметку {Environment.NewLine}" +
                        $"/cancelAddNote - отменить создание заметки {Environment.NewLine}" +
                        $"/cancelDeleteNote - отменить удаление заметки {Environment.NewLine}",
                        replyParameters: message.MessageId);
                    return;
                }
                else
                {
                    await client.SendMessage(message.Chat.Id, $"Ивините, не могу опознать команду... Пожалуйста, ознакомьтесь со списком: /help");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static async Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine($"Error occurred: {exception.Message}");
        }
    }


    public class NoteManager
    {

        private List<string> notes;
        private string filePath; 

        public NoteManager(long userId)
        {
            filePath = $"..\\..\\..\\notes\\{userId}.txt"; 
            notes = LoadNotes();
        }


        public List<string> LoadNotes()
        {
            if (SystemFile.Exists(filePath))
            {
                notes = SystemFile.ReadAllLines(filePath).ToList();
            }
            else notes = new List<string>();
            return notes;
        }
           

        public void AddNote(string text)
        {
            notes.Add(text);
            SaveNotes();
            Console.WriteLine($"Заметка добавлена: {text}");
        }

        public void DeleteNote(int id)
        {
            if (id >= 0 && id < notes.Count)
            {
                Console.WriteLine($"Заметка удалена: {notes[id]}");
                notes.RemoveAt(id);
                SaveNotes();
            }
            else
            {
                Console.WriteLine("Некорректный идентификатор заметки.");
            }
        }

        public List<string> GetAllNotes()
        {
            return notes;
        }

        private void SaveNotes()
        {
            try
            {
                if (!SystemFile.Exists(filePath))
                {
                    SystemFile.Create(filePath).Close(); // закрываем поток сразу
                }

                SystemFile.WriteAllLines(filePath, notes);
            }
            catch (IOException e)
            {
                Console.WriteLine("Exception: " + e);
            }
        }
    }
}
