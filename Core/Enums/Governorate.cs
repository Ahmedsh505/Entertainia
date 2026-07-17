using System.ComponentModel.DataAnnotations;

namespace Core.Enums
{
    /// <summary>
    /// Egyptian Governorates (Muhafazat) - 27 administrative divisions
    /// </summary>
    public enum Governorate
    {
        [Display(Name = "Alexandria", ShortName = "الإسكندرية")]
        Alexandria = 1,

        [Display(Name = "Aswan", ShortName = "أسوان")]
        Aswan = 2,

        [Display(Name = "Asyut", ShortName = "أسيوط")]
        Asyut = 3,

        [Display(Name = "Beheira", ShortName = "البحيرة")]
        Beheira = 4,

        [Display(Name = "Beni Suef", ShortName = "بني سويف")]
        BeniSuef = 5,

        [Display(Name = "Cairo", ShortName = "القاهرة")]
        Cairo = 6,

        [Display(Name = "Dakahlia", ShortName = "الدقهلية")]
        Dakahlia = 7,

        [Display(Name = "Damietta", ShortName = "دمياط")]
        Damietta = 8,

        [Display(Name = "Faiyum", ShortName = "الفيوم")]
        Faiyum = 9,

        [Display(Name = "Gharbia", ShortName = "الغربية")]
        Gharbia = 10,

        [Display(Name = "Giza", ShortName = "الجيزة")]
        Giza = 11,

        [Display(Name = "Ismailia", ShortName = "الإسماعيلية")]
        Ismailia = 12,

        [Display(Name = "Kafr El Sheikh", ShortName = "كفر الشيخ")]
        KafrElSheikh = 13,

        [Display(Name = "Luxor", ShortName = "الأقصر")]
        Luxor = 14,

        [Display(Name = "Matrouh", ShortName = "مطروح")]
        Matrouh = 15,

        [Display(Name = "Minya", ShortName = "المنيا")]
        Minya = 16,

        [Display(Name = "Monufia", ShortName = "المنوفية")]
        Monufia = 17,

        [Display(Name = "New Valley", ShortName = "الوادي الجديد")]
        NewValley = 18,

        [Display(Name = "North Sinai", ShortName = "شمال سيناء")]
        NorthSinai = 19,

        [Display(Name = "Port Said", ShortName = "بورسعيد")]
        PortSaid = 20,

        [Display(Name = "Qalyubia", ShortName = "القليوبية")]
        Qalyubia = 21,

        [Display(Name = "Qena", ShortName = "قنا")]
        Qena = 22,

        [Display(Name = "Red Sea", ShortName = "البحر الأحمر")]
        RedSea = 23,

        [Display(Name = "Sharqia", ShortName = "الشرقية")]
        Sharqia = 24,

        [Display(Name = "Sohag", ShortName = "سوهاج")]
        Sohag = 25,

        [Display(Name = "South Sinai", ShortName = "جنوب سيناء")]
        SouthSinai = 26,

        [Display(Name = "Suez", ShortName = "السويس")]
        Suez = 27
    }
}