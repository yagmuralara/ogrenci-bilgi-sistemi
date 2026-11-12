namespace OBSProjesi1.Models
{
    /// <summary>
    /// Öğrencinin akademik not bilgilerini (Vize, Final) tutan ve geçerlilik kontrolü yapan sınıftır.
    /// </summary>
    public class NotBilgisi
    {
        public int OgrNo { get; set; }
        public int DersID { get; set; }
        public int Vize { get; set; }
        public int Final { get; set; }

        /// <summary>
        /// Girilen vize veya final notunun 0-100 sınırları dahilinde olup olmadığını doğrular.
        /// </summary>
        /// <param name="notDegeri">Kontrol edilecek sayısal not puanı.</param>
        /// <returns>Not geçerli aralıktaysa true, değilse false döner.</returns>
        public bool NotKontrol(int notDegeri)
        {
            return notDegeri >= 0 && notDegeri <= 100;
        }
    }

    /// <summary>
    /// Üniversite bünyesindeki program bilgilerini (Önlisans, Lisans vb.) ve bağlı olduğu bölümü tutan sınıftır.
    /// </summary>
    /// <remarks>C# dilinin 'program' anahtar kelimesiyle çakışmayı önlemek için 'ProgramBilgisi' olarak adlandırılmıştır.</remarks>
    public class ProgramBilgisi
    {
        public int ProgramID { get; set; }
        public string ProgramAdi { get; set; } = string.Empty;
        public int BolumID { get; set; } // Hangi bölüme bağlı olduğunu belirten yabancı anahtar (FK)
    }

    /// <summary>
    /// Okulda görevli olan öğretim görevlilerinin (Akademisyen) kişisel, unvan ve iletişim bilgilerini tutar.
    /// </summary>
    public class Akademisyen
    {
        public int AkademisyenID { get; set; }
        public string SicilNo { get; set; } = string.Empty; // Kurumsal sicil numarası
        public string Unvan { get; set; } = string.Empty;   
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sisteme kayıtlı öğrencilerin temel kimlik, iletişim, bölüm ve program ilişkilerini tutan sınıftır.
    /// </summary>
    public class Ogrenci
    {
        public int OgrNo { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int BolumID { get; set; }    // Öğrencinin kayıtlı olduğu bölümün ID'si
        public int ProgramID { get; set; }  // Öğrencinin eğitim gördüğü programın ID'si
    }

    /// <summary>
    /// Müfredatta yer alan derslerin kodunu, adını, AKTS değerini ve dersi veren akademisyen eşleşmesini tutar.
    /// </summary>
    public class Ders
    {
        public int DersID { get; set; }
        public string DersKodu { get; set; } = string.Empty; // Örn: BLP102
        public string DersAdi { get; set; } = string.Empty;
        public int AKTS { get; set; }
        public int AkademisyenID { get; set; } // Dersi yürüten akademisyenin ID'si (FK)
    }

    /// <summary>
    /// Üniversitedeki akademik departmanları (Bilgisayar Teknolojileri, Yönetim Bilişim vb.) temsil eden sınıftır.
    /// </summary>
    public class Bolum
    {
        public int BolumID { get; set; }
        public string BolumAdi { get; set; } = string.Empty;
    }
}