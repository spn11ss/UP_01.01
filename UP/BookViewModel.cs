using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace UP
{
    /// <summary>
    /// Модель представления для книги (ViewModel)
    /// </summary>
    /// <remarks>
    /// Класс служит промежуточным звеном между данными из базы данных и пользовательским интерфейсом.
    /// Используется для передачи данных о книге на различные страницы приложения:
    /// - CatalogPage (каталог книг)
    /// - ReadingListsPage (списки книг)
    /// - BookWindow (страница детального просмотра)
    /// - AuthorPage (страница автора)
    /// 
    /// Преимущества использования ViewModel:
    /// - Объединяет данные из нескольких таблиц (Books, Users, Reviews, ReadingLists)
    /// - Добавляет вычисляемые поля (AvgRating, ReviewsCount)
    /// - Изолирует представление от структуры базы данных
    /// </remarks>
    public class BookViewModel
    {
        public int ID { get; set; }
        public string BookName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string AuthorName { get; set; }
        public bool IsFrozen { get; set; }
        public double AvgRating { get; set; }
        public int ReviewsCount { get; set; }
        public int ReadingListId { get; set; }
        public int CurrentStatusId { get; set; }
        public string Text { get; set; }
        public string Genres { get; set; }
        public string Rating { get; set; }

    }
}
