using System;
using System.Data.SqlClient;
using OBSProjesi1.Models;

namespace OBSProjesi1
{
    class Program
    {
        // Uygulamanın ana giriş noktası ve ana menü döngüsü
        static void Main(string[] args)
        {
            // Veritabanı bağlantı yardımı için yardımcı sınıftan nesne üretilir
            DatabaseHelper db = new DatabaseHelper();

            // Kullanıcı çıkış yapana kadar uygulamanın açık kalmasını sağlayan sonsuz döngü
            while (true)
            {
                Console.Clear();
                Console.WriteLine("===========================================");
                Console.WriteLine("               OBS SİSTEMİ");
                Console.WriteLine("===========================================");
                Console.WriteLine("1 - Öğrenci Girişi (Sadece Not/Ders Görüntüleme)");
                Console.WriteLine("2 - Akademisyen / Yönetim Girişi (Tam Yetki)");
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("0 - Çıkış");
                Console.Write("\nSeçiminiz: ");

                // Kullanıcı girişinin sayısal doğrulaması yapılır
                int secim;
                if (!int.TryParse(Console.ReadLine(), out secim))
                {
                    Console.WriteLine("\nGeçersiz bir sayı girdiniz.");
                    Console.ReadKey();
                    continue;
                }

                // Seçilen role göre ilgili panele yönlendirme yapılır
                switch (secim)
                {
                    case 1:
                        OgrenciGirisPaneli(db);
                        break;
                    case 2:
                        AkademisyenGirisPaneli(db);
                        break;
                    case 0:
                        Environment.Exit(0); // Programı tamamen sonlandırır
                        break;
                    default:
                        Console.WriteLine("Hatalı seçim! Tekrar deneyin.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        // ================= GİRİŞ PANELLERİ ROLLERİ =================

        // Öğrencilerin sadece kendi akademik durumlarını sorgulayabileceği panel
        static void OgrenciGirisPaneli(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("==== ÖĞRENCİ GİRİŞİ ====");
            Console.Write("Öğrenci Numaranızı Giriniz: ");

            int ogrNo;
            if (!int.TryParse(Console.ReadLine(), out ogrNo))
            {
                Console.WriteLine("\nHatalı numara formatı!");
                Console.ReadKey();
                return;
            }

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                // Girilen numaraya ait bir öğrenci var mı kontrolü ve isim çekme işlemi
                SqlCommand kontrolCmd = new SqlCommand("SELECT Ad, Soyad FROM Ogrenci WHERE OgrNo=@o", conn);
                kontrolCmd.Parameters.AddWithValue("@o", ogrNo);
                SqlDataReader drOgr = kontrolCmd.ExecuteReader();

                if (drOgr.Read())
                {
                    string adSoyad = $"{drOgr["Ad"]} {drOgr["Soyad"]}";
                    drOgr.Close(); // Yeni bir sorgu çalıştırmadan önce açık olan reader kapatılır

                    Console.Clear();
                    Console.WriteLine("==============================================================================================");
                    Console.WriteLine($" HOŞ GELDİNİZ: {adSoyad} ({ogrNo})");
                    Console.WriteLine("==============================================================================================");

                    // Öğrencinin aldığı dersleri, ders notlarını ve dersin hocasını getiren ilişkili sorgu
                    string query = @"
                        SELECT 
                            d.DersKodu, d.DersAdi, d.AKTS,
                            a.Unvan + ' ' + a.Ad + ' ' + a.Soyad AS Akademisyen,
                            n.Vize, n.Final, n.Ortalama, n.HarfNotu
                        FROM Notlar n
                        INNER JOIN Ders d ON n.DersID = d.DersID
                        INNER JOIN Akademisyen a ON d.AkademisyenID = a.AkademisyenID
                        WHERE n.OgrNo = @o";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@o", ogrNo);
                    SqlDataReader dr = cmd.ExecuteReader();

                    Console.WriteLine("Ders Kodu".PadRight(12) + "Ders Adı".PadRight(30) + "Akademisyen".PadRight(25) + "AKTS".PadRight(8) + "Vize".PadRight(8) + "Final".PadRight(8) + "Ort".PadRight(8) + "Harf");
                    Console.WriteLine(new string('-', 110));

                    bool kayitVarMi = false;
                    // Veritabanından gelen ders ve not verileri ekrana yazdırılır
                    while (dr.Read())
                    {
                        kayitVarMi = true;
                        Console.WriteLine(
                            dr["DersKodu"].ToString().PadRight(12) +
                            dr["DersAdi"].ToString().PadRight(30) +
                            dr["Akademisyen"].ToString().PadRight(25) +
                            dr["AKTS"].ToString().PadRight(8) +
                            dr["Vize"].ToString().PadRight(8) +
                            dr["Final"].ToString().PadRight(8) +
                            dr["Ortalama"].ToString().PadRight(8) +
                            dr["HarfNotu"].ToString()
                        );
                    }
                    dr.Close();

                    if (!kayitVarMi)
                    {
                        Console.WriteLine("\n[-] Henüz adınıza kayıtlı bir ders veya not bulunamadı.");
                    }
                    else
                    {
                        // AKTS ağırlıklı akademik genel ortalamayı (4'lük sistem) hesaplayan sorgu
                        string ortalamaQuery = @"
                            SELECT CAST(SUM(n.DortlukSistem * d.AKTS) / SUM(d.AKTS) AS DECIMAL(10,2))
                            FROM Notlar n
                            INNER JOIN Ders d ON n.DersID = d.DersID
                            WHERE n.OgrNo = @o";

                        SqlCommand ortCmd = new SqlCommand(ortalamaQuery, conn);
                        ortCmd.Parameters.AddWithValue("@o", ogrNo);
                        object donemOrt = ortCmd.ExecuteScalar();

                        Console.WriteLine(new string('-', 110));
                        Console.WriteLine("Dönem Ortalaması (AKTS'li 4'lük Sistem): " + (donemOrt == DBNull.Value ? "0.00" : donemOrt));
                    }
                }
                else
                {
                    drOgr.Close();
                    Console.WriteLine("\n[-] Sistemde bu numaraya ait bir öğrenci bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Bir hata oluştu: " + ex.Message);
            }
            finally
            {
                conn.Close(); // Veritabanı bağlantısı güvenli bir şekilde kapatılır
            }

            Console.WriteLine("\nAna menüye dönmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Yönetici ve akademisyenler için şifreli giriş kontrol paneli
        static void AkademisyenGirisPaneli(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("==== AKADEMİSYEN / YÖNETİM GİRİŞİ ====");
            Console.WriteLine("Şifre=1234");
            Console.Write("Giriş Şifresi: ");
            string sifre = Console.ReadLine();

            if (sifre != "1234")
            {
                Console.WriteLine("\n[-] Hatalı Şifre! Yetkiniz yok.");
                Console.ReadKey();
                return;
            }

            // Şifre doğru ise tam yetkili yönetim ana menüsüne geçiş yapılır
            YonetimAnaMenu(db);
        }

        // Tüm CRUD işlemlerinin alt menülerine ulaşılan ana yönetim merkezi
        static void YonetimAnaMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("===========================================");
                Console.WriteLine("          YÖNETİCİ / AKADEMİSYEN PANELİ");
                Console.WriteLine("===========================================");
                Console.WriteLine("1 - Not İşlemleri");
                Console.WriteLine("2 - Akademisyen İşlemleri");
                Console.WriteLine("3 - Öğrenci İşlemleri");
                Console.WriteLine("4 - Ders İşlemleri");
                Console.WriteLine("5 - Bölüm İşlemleri");
                Console.WriteLine("6 - Program İşlemleri");
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("0 - Oturumu Kapat (Ana Menüye Dön)");
                Console.Write("\nSeçiminiz: ");

                int secim;
                if (!int.TryParse(Console.ReadLine(), out secim)) continue;

                switch (secim)
                {
                    case 1: NotMenu(db); break;
                    case 2: AkademisyenMenu(db); break;
                    case 3: OgrenciMenu(db); break;
                    case 4: DersMenu(db); break;
                    case 5: BolumMenu(db); break;
                    case 6: ProgramMenu(db); break;
                    case 0: return; // Bu metottan çıkarak Main'deki ilk ana menüye geri döner
                }
            }
        }

        // ================= NOT İŞLEMLERİ =================

        // Not yönetimi alt menüsü
        static void NotMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== NOT İŞLEMLERİ ====");
                Console.WriteLine("1- Not Ekle");
                Console.WriteLine("2- Not Sil");
                Console.WriteLine("3- Not Güncelle");
                Console.WriteLine("4- Tüm Notları Listele");
                Console.WriteLine("0- Geri");
                Console.Write("\nSeçiminiz: ");

                int secim;
                if (!int.TryParse(Console.ReadLine(), out secim)) continue;

                switch (secim)
                {
                    case 1: NotEkle(db); break;
                    case 2: NotSil(db); break;
                    case 3: NotGuncelle(db); break;
                    case 4: Listele(db); break;
                    case 0: return;
                }
            }
        }

        // Veritabanına yeni vize ve final notu ekleme fonksiyonu
        static void NotEkle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- NOT EKLE ---");
            NotBilgisi nb = new NotBilgisi();
            SqlConnection conn = db.BaglantiGetir();

            Console.Write("Ogr No: ");
            nb.OgrNo = Convert.ToInt32(Console.ReadLine());

            int dersId;
            Console.Write("Ders ID (1-8 arası sayı girin): ");
            while (!int.TryParse(Console.ReadLine(), out dersId))
            {
                Console.Write("Hatalı giriş! Tekrar Ders ID: ");
            }
            nb.DersID = dersId;

            int vize;
            Console.Write("Vize: ");
            while (!int.TryParse(Console.ReadLine(), out vize) || vize < 0 || vize > 100)
            {
                Console.Write("0-100 arası geçerli Vize gir: ");
            }
            nb.Vize = vize;

            int final;
            Console.Write("Final: ");
            while (!int.TryParse(Console.ReadLine(), out final) || final < 0 || final > 100)
            {
                Console.Write("0-100 arası geçerli Final gir: ");
            }
            nb.Final = final;

            try
            {
                conn.Open();
                string query = "INSERT INTO Notlar(OgrNo,DersID,Vize,Final) VALUES(@OgrNo,@DersID,@Vize,@Final)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@OgrNo", nb.OgrNo);
                cmd.Parameters.AddWithValue("@DersID", nb.DersID);
                cmd.Parameters.AddWithValue("@Vize", nb.Vize);
                cmd.Parameters.AddWithValue("@Final", nb.Final);
                cmd.ExecuteNonQuery();

                Console.WriteLine("\n[+] Not başarıyla eklendi.");
            }
            catch (SqlException ex)
            {
                // SQL Server Yabancı Anahtar (Foreign Key) kısıtlama ihlali kontrolü
                if (ex.Number == 547)
                    Console.WriteLine("\n[-] Hata: Girdiğiniz Öğrenci No veya Ders ID veri tabanında yok!");
                else
                    Console.WriteLine("\n[-] Veri tabanı hatası: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Kayıtlı bir not bilgisini veritabanından tamamen siler
        static void NotSil(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- NOT SİL ---");
            Console.Write("Ogr No: "); int o = Convert.ToInt32(Console.ReadLine());
            Console.Write("Ders ID: "); int d = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Notlar WHERE OgrNo=@o AND DersID=@d", conn);
                cmd.Parameters.AddWithValue("@o", o);
                cmd.Parameters.AddWithValue("@d", d);
                int sonuc = cmd.ExecuteNonQuery();

                if (sonuc > 0)
                    Console.WriteLine("\n[+] Not başarıyla silindi.");
                else
                    Console.WriteLine("\n[-] Uyarı: Silinecek uygun kayıt bulunamadı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Hata oluştu: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Var olan bir kaydın vize ve final notlarını günceller
        static void NotGuncelle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- NOT GÜNCELLE ---");
            Console.Write("Ogr No: "); int o = Convert.ToInt32(Console.ReadLine());
            Console.Write("Ders ID: "); int d = Convert.ToInt32(Console.ReadLine());
            Console.Write("Yeni Vize: "); int v = Convert.ToInt32(Console.ReadLine());
            Console.Write("Yeni Final: "); int f = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Notlar SET Vize=@v,Final=@f WHERE OgrNo=@o AND DersID=@d", conn);
                cmd.Parameters.AddWithValue("@v", v);
                cmd.Parameters.AddWithValue("@f", f);
                cmd.Parameters.AddWithValue("@o", o);
                cmd.Parameters.AddWithValue("@d", d);
                int sonuc = cmd.ExecuteNonQuery();

                if (sonuc > 0)
                    Console.WriteLine("\n[+] Not başarıyla güncellendi.");
                else
                    Console.WriteLine("\n[-] Uyarı: Güncellenecek kayıt bulunamadı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Hata oluştu: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Sistemdeki tüm öğrencilerin not dökümünü listeler
        static void Listele(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- TÜM NOTLARIN LİSTESİ ---");
            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT n.OgrNo, o.Ad, o.Soyad, d.DersAdi, n.Vize, n.Final, n.Ortalama, n.HarfNotu
                                                  FROM Notlar n
                                                  JOIN Ogrenci o ON n.OgrNo=o.OgrNo
                                                  JOIN Ders d ON n.DersID=d.DersID", conn);

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Console.WriteLine($"Öğr No: {dr[0]} | {dr[1]} {dr[2]} | Ders: {dr[3]} | Vize: {dr[4]} | Final: {dr[5]} | Ort: {dr[6]} | Harf: {dr[7]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Listeleme hatası: " + ex.Message);
            }
            
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // ================= AKADEMİSYEN İŞLEMLERİ =================

        // Akademisyen yönetim alt menüsü
        static void AkademisyenMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== AKADEMİSYEN İŞLEMLERİ ====");
                Console.WriteLine("1- Akademisyen Ekle");
                Console.WriteLine("2- Akademisyen Sil (Sicil No ile)");
                Console.WriteLine("3- Akademisyen Güncelle");
                Console.WriteLine("4- Akademisyenleri Listele");
                Console.WriteLine("0- Geri");
                Console.Write("\nSeçiminiz: ");

                int s;
                if (!int.TryParse(Console.ReadLine(), out s)) continue;

                if (s == 1) AkademisyenEkle(db);
                else if (s == 2) AkademisyenSil(db);
                else if (s == 3) AkademisyenGuncelle(db);
                else if (s == 4) AkademisyenListele(db);
                else if (s == 0) return;
            }
        }

        // Veritabanına yeni akademisyen kaydı ekler
        static void AkademisyenEkle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- AKADEMİSYEN EKLE ---");
            SqlConnection conn = db.BaglantiGetir();

            Console.Write("Sicil No: "); string sicil = Console.ReadLine();
            Console.Write("Unvan: "); string unvan = Console.ReadLine();
            Console.Write("Ad: "); string ad = Console.ReadLine();
            Console.Write("Soyad: "); string soyad = Console.ReadLine();
            Console.Write("Email: "); string email = Console.ReadLine();

            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Akademisyen(SicilNo,Unvan,Ad,Soyad,Email) VALUES(@s,@u,@a,@so,@e)", conn);

                cmd.Parameters.AddWithValue("@s", sicil);
                cmd.Parameters.AddWithValue("@u", unvan);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@so", soyad);
                cmd.Parameters.AddWithValue("@e", email);

                cmd.ExecuteNonQuery();
                Console.WriteLine("\n[+] Akademisyen başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Ekleme hatası: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Kurumsal sicil numarasına göre akademisyen kaydını siler
        static void AkademisyenSil(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- AKADEMİSYEN SİL ---");
            Console.Write("Silinecek Akademisyenin Sicil No'su: ");
            string sicil = Console.ReadLine();

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Akademisyen WHERE SicilNo=@sicil", conn);
                cmd.Parameters.AddWithValue("@sicil", sicil);
                int sonuc = cmd.ExecuteNonQuery();

                if (sonuc > 0)
                    Console.WriteLine("\n[+] Akademisyen başarıyla silindi.");
                else
                    Console.WriteLine("\n[-] Belirtilen Sicil No ile akademisyen bulunamadı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Hata: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Sicil numarası baz alınarak ilgili akademisyenin bilgilerini günceller
        static void AkademisyenGuncelle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- AKADEMİSYEN GÜNCELLE ---");
            Console.Write("Güncellenecek Akademisyenin Sicil No'su: ");
            string sicil = Console.ReadLine();

            Console.Write("Yeni Unvan: "); string unvan = Console.ReadLine();
            Console.Write("Yeni Ad: "); string ad = Console.ReadLine();
            Console.Write("Yeni Soyad: "); string soyad = Console.ReadLine();
            Console.Write("Yeni Email: "); string email = Console.ReadLine();

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Akademisyen SET Unvan=@u, Ad=@a, Soyad=@so, Email=@e WHERE SicilNo=@s", conn);

                cmd.Parameters.AddWithValue("@s", sicil);
                cmd.Parameters.AddWithValue("@u", unvan);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@so", soyad);
                cmd.Parameters.AddWithValue("@e", email);

                int sonuc = cmd.ExecuteNonQuery();
                if (sonuc > 0)
                    Console.WriteLine("\n[+] Akademisyen bilgileri güncellendi.");
                else
                    Console.WriteLine("\n[-] Güncellenecek akademisyen bulunamadı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Hata: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // Veritabanındaki tüm akademisyenleri listeler
        static void AkademisyenListele(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- AKADEMİSYEN LİSTESİ ---");
            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT SicilNo, Unvan, Ad, Soyad, Email FROM Akademisyen", conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Console.WriteLine($"Sicil: {dr[0]} | {dr[1]} {dr[2]} {dr[3]} | E-posta: {dr[4]}");
                }
            }
            catch (Exception ex) { Console.WriteLine("[-] Listeleme hatası: " + ex.Message); }
            finally { conn.Close(); }

            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }

        // ================= ÖĞRENCİ İŞLEMLERİ =================

        // Öğrenci yönetim alt menüsü
        static void OgrenciMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== ÖĞRENCİ İŞLEMLERİ ====");
                Console.WriteLine("1- Öğrenci Ekle");
                Console.WriteLine("2- Öğrenci Sil");
                Console.WriteLine("3- Öğrenci Güncelle");
                Console.WriteLine("4- Öğrenci Listele");
                Console.WriteLine("0- Geri");
                Console.Write("\nSeçiminiz: ");

                int s;
                if (!int.TryParse(Console.ReadLine(), out s)) continue;

                if (s == 1) OgrenciEkle(db);
                else if (s == 2) OgrenciSil(db);
                else if (s == 3) OgrenciGuncelle(db);
                else if (s == 4) OgrenciListele(db);
                else if (s == 0) return;
            }
        }

        // Yeni öğrenci kaydı oluşturur
        static void OgrenciEkle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- ÖĞRENCİ EKLE ---");
            SqlConnection conn = db.BaglantiGetir();

            Console.Write("Öğrenci No: "); int ogrNo = Convert.ToInt32(Console.ReadLine());
            Console.Write("Ad: "); string ad = Console.ReadLine();
            Console.Write("Soyad: "); string soyad = Console.ReadLine();
            Console.Write("TC No: "); string tc = Console.ReadLine();
            Console.Write("Email: "); string email = Console.ReadLine();
            Console.Write("Telefon: "); string tel = Console.ReadLine();
            Console.Write("Program ID: "); int progId = Convert.ToInt32(Console.ReadLine());
            Console.Write("Bölüm ID: "); int bolId = Convert.ToInt32(Console.ReadLine());

            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Ogrenci(OgrNo,Ad,Soyad,TCNo,Email,Telefon,ProgramID,BolumID) VALUES(@o,@a,@so,@t,@e,@tel,@p,@b)", conn);

                cmd.Parameters.AddWithValue("@o", ogrNo);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@so", soyad);
                cmd.Parameters.AddWithValue("@t", tc);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@tel", tel);
                cmd.Parameters.AddWithValue("@p", progId);
                cmd.Parameters.AddWithValue("@b", bolId);

                cmd.ExecuteNonQuery();
                Console.WriteLine("\n[+] Öğrenci başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Ekleme hatası (Bölüm ID veya Program ID kontrol edin): " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
            Console.ReadKey();
        }

        // Öğrenci numarasına göre öğrenci kaydını sistemden kaldırır
        static void OgrenciSil(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- ÖĞRENCİ SİL ---");
            Console.Write("Silinecek Öğrenci No: "); int ogrNo = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Ogrenci WHERE OgrNo=@o", conn);
                cmd.Parameters.AddWithValue("@o", ogrNo);
                int r = cmd.ExecuteNonQuery();
                if (r > 0) Console.WriteLine("\n[+] Öğrenci silindi.");
                else Console.WriteLine("\n[-] Öğrenci bulunamadı.");
            }
            catch (SqlException ex)
            {
                // İlişkili tabloda (Notlar) veri varsa silme işlemini engelleyen güvenli hata yönetimi
                if (ex.Number == 547)
                    Console.WriteLine("\n[-] Hata: Bu öğrenciye ait sistemde not kayıtları var! Önce notlarını silmelisiniz.");
                else
                    Console.WriteLine("\n[-] Hata: " + ex.Message);
            }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Belirtilen öğrenci numarasına sahip öğrencinin detaylarını günceller
        static void OgrenciGuncelle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- ÖĞRENCİ GÜNCELLE ---");
            Console.Write("Güncellenecek Öğrenci No: "); int ogrNo = Convert.ToInt32(Console.ReadLine());

            Console.Write("Yeni Ad: "); string ad = Console.ReadLine();
            Console.Write("Yeni Soyad: "); string soyad = Console.ReadLine();
            Console.Write("Yeni Email: "); string email = Console.ReadLine();
            Console.Write("Yeni Telefon: "); string tel = Console.ReadLine();
            Console.Write("Yeni Program ID: "); int progId = Convert.ToInt32(Console.ReadLine());
            Console.Write("Yeni Bölüm ID: "); int bolId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Ogrenci SET Ad=@a, Soyad=@so, Email=@e, Telefon=@tel, ProgramID=@p, BolumID=@b WHERE OgrNo=@o", conn);

                cmd.Parameters.AddWithValue("@o", ogrNo);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@so", soyad);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@tel", tel);
                cmd.Parameters.AddWithValue("@p", progId);
                cmd.Parameters.AddWithValue("@b", bolId);

                int r = cmd.ExecuteNonQuery();
                if (r > 0) Console.WriteLine("\n[+] Öğrenci bilgileri başarıyla güncellendi.");
                else Console.WriteLine("\n[-] Güncellenecek öğrenci bulunamadı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[-] Hata (Program veya Bölüm ID kontrol edin): " + ex.Message);
            }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Sistemde kayıtlı olan tüm öğrencilerin temel listesini döner
        static void OgrenciListele(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- ÖĞRENCİ LİSTESİ ---");
            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT OgrNo, Ad, Soyad, Email FROM Ogrenci", conn);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Console.WriteLine($"No: {dr[0]} | {dr[1]} {dr[2]} | Mail: {dr[3]}");
                }
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata: " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // ================= DERS İŞLEMLERİ =================

        // Ders yönetim alt menüsü
        static void DersMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== DERS İŞLEMLERİ ====");
                Console.WriteLine("1- Ders Ekle");
                Console.WriteLine("2- Ders Sil");
                Console.WriteLine("3- Ders Güncelle");
                Console.WriteLine("4- Ders Listele");
                Console.WriteLine("0- Geri");
                Console.Write("\nSeçiminiz: ");

                int s;
                if (!int.TryParse(Console.ReadLine(), out s)) continue;

                if (s == 1) DersEkle(db);
                else if (s == 2) DersSil(db);
                else if (s == 3) DersGuncelle(db);
                else if (s == 4) DersListele(db);
                else if (s == 0) return;
            }
        }

        // Akademisyen ve AKTS yükümlülüklerine bağlı yeni ders tanımlar
        static void DersEkle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- DERS EKLE ---");
            SqlConnection conn = db.BaglantiGetir();

            Console.Write("Ders Kodu (Örn: BLP108): "); string kod = Console.ReadLine();
            Console.Write("Ders Adı: "); string ad = Console.ReadLine();
            Console.Write("AKTS: "); int akts = Convert.ToInt32(Console.ReadLine());
            Console.Write("Akademisyen ID: "); int akaId = Convert.ToInt32(Console.ReadLine());

            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Ders(DersKodu,DersAdi,AKTS,AkademisyenID) VALUES(@k,@a,@ak,@aka)", conn);
                cmd.Parameters.AddWithValue("@k", kod);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@ak", akts);
                cmd.Parameters.AddWithValue("@aka", akaId);
                cmd.ExecuteNonQuery();
                Console.WriteLine("\n[+] Ders eklendi.");
            }
            catch (Exception ex) { Console.WriteLine("\n[-] Hata (Akademisyen ID kontrol edin): " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Benzersiz ders kimlik numarasına (DersID) göre ders siler
        static void DersSil(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- DERS SİL ---");
            Console.Write("Silinecek Dersin ID Değeri: ");
            int dersId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Ders WHERE DersID=@id", conn);
                cmd.Parameters.AddWithValue("@id", dersId);

                int sonuc = cmd.ExecuteNonQuery();
                if (sonuc > 0)
                    Console.WriteLine("\n[+] Ders başarıyla silindi.");
                else
                    Console.WriteLine("\n[-] Belirtilen ID ile eşleşen ders bulunamadı.");
            }
            catch (SqlException ex)
            {
                // İlişkili not kayıtları olan derslerin silinmesini engelleyen kontrol yapısı
                if (ex.Number == 547)
                    Console.WriteLine("\n[-] Hata: Bu derse ait sistemde kayıtlı notlar var! Önce notlarını silmelisiniz.");
                else
                    Console.WriteLine("\n[-] Hata oluştu: " + ex.Message);
            }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Sistemdeki mevcut dersin kod, ad, AKTS veya akademisyen eşleşmesini günceller
        static void DersGuncelle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- DERS GÜNCELLE ---");
            Console.Write("Güncellenecek Dersin ID Değeri: ");
            int dersId = Convert.ToInt32(Console.ReadLine());

            Console.Write("Yeni Ders Kodu: "); string kod = Console.ReadLine();
            Console.Write("Yeni Ders Adı: "); string ad = Console.ReadLine();
            Console.Write("Yeni AKTS: "); int akts = Convert.ToInt32(Console.ReadLine());
            Console.Write("Yeni Akademisyen ID: "); int akaId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Ders SET DersKodu=@k, DersAdi=@a, AKTS=@ak, AkademisyenID=@aka WHERE DersID=@id", conn);

                cmd.Parameters.AddWithValue("@id", dersId);
                cmd.Parameters.AddWithValue("@k", kod);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@ak", akts);
                cmd.Parameters.AddWithValue("@aka", akaId);

                int sonuc = cmd.ExecuteNonQuery();
                if (sonuc > 0)
                    Console.WriteLine("\n[+] Ders bilgileri başarıyla güncellendi.");
                else
                    Console.WriteLine("\n[-] Güncellenecek ders bulunamadı.");
            }
            catch (Exception ex) { Console.WriteLine("\n[-] Hata (Akademisyen ID kontrol edin): " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Açılan ve aktif olan tüm dersleri listeler
        static void DersListele(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- DERS LİSTESİ ---");
            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT DersID, DersKodu, DersAdi, AKTS FROM Ders", conn);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Console.WriteLine($"ID: {dr[0]} | Kod: {dr[1]} | Ad: {dr[2]} | AKTS: {dr[3]}");
                }
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata: " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // ================= BÖLÜM İŞLEMLERİ =================

        // Akademik bölüm yönetim alt menüsü
        static void BolumMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== BÖLÜM İŞLEMLERİ ====");
                Console.WriteLine("1- Bölüm Ekle");
                Console.WriteLine("2- Bölüm Sil");
                Console.WriteLine("3- Bölüm Güncelle");
                Console.WriteLine("4- Bölüm Listele");
                Console.WriteLine("0- Geri");
                Console.Write("\nSeçiminiz: ");

                int s;
                if (!int.TryParse(Console.ReadLine(), out s)) continue;

                if (s == 1) BolumEkle(db);
                else if (s == 2) BolumSil(db);
                else if (s == 3) BolumGuncelle(db);
                else if (s == 4) BolumListele(db);
                else if (s == 0) return;
            }
        }

        // Fakülte/Yüksekokul bünyesine yeni bir bölüm ekler
        static void BolumEkle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- BÖLÜM EKLE ---");
            Console.Write("Bölüm Adı: "); string ad = Console.ReadLine();

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Bolum(BolumAdi) VALUES(@a)", conn);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.ExecuteNonQuery();
                Console.WriteLine("\n[+] Bölüm eklendi.");
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata: " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Belirtilen kimliğe sahip bölümü siler
        static void BolumSil(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- BÖLÜM SİL ---");
            Console.Write("Silinecek Bölüm ID: "); int bolId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Bolum WHERE BolumID=@id", conn);
                cmd.Parameters.AddWithValue("@id", bolId);
                int r = cmd.ExecuteNonQuery();

                if (r > 0) Console.WriteLine("\n[+] Bölüm başarıyla silindi.");
                else Console.WriteLine("\n[-] Belirtilen ID ile bölüm bulunamadı.");
            }
            catch (SqlException ex)
            {
                // Bölüme bağlı alt sınıflar veya öğrenciler varsa silmeyi reddeden kısıtlama kontrolü
                if (ex.Number == 547)
                    Console.WriteLine("\n[-] Hata: Bu bölüme bağlı programlar veya öğrenciler var! Önce onları silmelisiniz.");
                else
                    Console.WriteLine("\n[-] Hata: " + ex.Message);
            }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Kayıtlı olan bir bölümün adını revize eder
        static void BolumGuncelle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- BÖLÜM GÜNCELLE ---");
            Console.Write("Güncellenecek Bölüm ID: "); int bolId = Convert.ToInt32(Console.ReadLine());
            Console.Write("Yeni Bölüm Adı: "); string yeniAd = Console.ReadLine();

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Bolum SET BolumAdi=@ad WHERE BolumID=@id", conn);
                cmd.Parameters.AddWithValue("@id", bolId);
                cmd.Parameters.AddWithValue("@ad", yeniAd);
                int r = cmd.ExecuteNonQuery();

                if (r > 0) Console.WriteLine("\n[+] Bölüm adı başarıyla güncellendi.");
                else Console.WriteLine("\n[-] Güncellenecek bölüm bulunamadı.");
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata: " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Mevcut tüm akademik bölümleri listeler
        static void BolumListele(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- BÖLÜM LİSTESİ ---");
            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT BolumID, BolumAdi FROM Bolum", conn);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Console.WriteLine($"Bölüm ID: {dr[0]} | Bölüm Adı: {dr[1]}");
                }
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata: " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // ================= PROGRAM İŞLEMLERİ =================

        // Akademik program (Önlisans/Lisans vb.) yönetim alt menüsü
        static void ProgramMenu(DatabaseHelper db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== PROGRAM İŞLEMLERİ ====");
                Console.WriteLine("1- Program Ekle");
                Console.WriteLine("2- Program Sil");
                Console.WriteLine("3- Program Güncelle");
                Console.WriteLine("4- Program Listele");
                Console.WriteLine("0- Geri");
                Console.Write("\nSeçiminiz: ");

                int s;
                if (!int.TryParse(Console.ReadLine(), out s)) continue;

                if (s == 1) ProgramEkle(db);
                else if (s == 2) ProgramSil(db);
                else if (s == 3) ProgramGuncelle(db);
                else if (s == 4) ProgramListele(db);
                else if (s == 0) return;
            }
        }

        // Belirli bir bölüme bağlı yeni eğitim programı ekler
        static void ProgramEkle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- PROGRAM EKLE ---");
            Console.Write("Program Adı (Örn: Önlisans / Lisans): "); string ad = Console.ReadLine();
            Console.Write("Bağlı Olduğu Bölüm ID: "); int bolId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Program(ProgramAdi, BolumID) VALUES(@a, @b)", conn);
                cmd.Parameters.AddWithValue("@a", ad);
                cmd.Parameters.AddWithValue("@b", bolId);
                cmd.ExecuteNonQuery();
                Console.WriteLine("\n[+] Program başarıyla eklendi.");
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata (Bölüm ID'sini kontrol edin): " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // ProgramID değerini eşleştirerek ilgili program kaydını siler
        static void ProgramSil(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- PROGRAM SİL ---");
            Console.Write("Silinecek Program ID: "); int progId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Program WHERE ProgramID=@id", conn);
                cmd.Parameters.AddWithValue("@id", progId);
                int r = cmd.ExecuteNonQuery();

                if (r > 0) Console.WriteLine("\n[+] Program başarıyla silindi.");
                else Console.WriteLine("\n[-] Belirtilen ID ile program bulunamadı.");
            }
            catch (SqlException ex)
            {
                // Programa kayıtlı öğrenciler varsa silme işlemini durduran kısıtlama yönetimi
                if (ex.Number == 547)
                    Console.WriteLine("\n[-] Hata: Bu programa bağlı öğrenciler mevcut! Önce öğrencileri silmelisiniz.");
                else
                    Console.WriteLine("\n[-] Hata: " + ex.Message);
            }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Mevcut programın adını veya bağlı olduğu bölümün kimlik bilgisini günceller
        static void ProgramGuncelle(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- PROGRAM GÜNCELLE ---");
            Console.Write("Güncellenecek Program ID: "); int progId = Convert.ToInt32(Console.ReadLine());
            Console.Write("Yeni Program Adı: "); string yeniAd = Console.ReadLine();
            Console.Write("Yeni Bölüm ID: "); int yeniBolId = Convert.ToInt32(Console.ReadLine());

            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Program SET ProgramAdi=@ad, BolumID=@bolId WHERE ProgramID=@id", conn);
                cmd.Parameters.AddWithValue("@id", progId);
                cmd.Parameters.AddWithValue("@ad", yeniAd);
                cmd.Parameters.AddWithValue("@bolId", yeniBolId);
                int r = cmd.ExecuteNonQuery();

                if (r > 0) Console.WriteLine("\n[+] Program başarıyla güncellendi.");
                else Console.WriteLine("\n[-] Güncellenecek program bulunamadı.");
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata (Bölüm ID kontrol edin): " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }

        // Açılmış olan tüm aktif eğitim programlarını listeler
        static void ProgramListele(DatabaseHelper db)
        {
            Console.Clear();
            Console.WriteLine("--- PROGRAM LİSTESİ ---");
            SqlConnection conn = db.BaglantiGetir();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT ProgramID, ProgramAdi, BolumID FROM Program", conn);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Console.WriteLine($"Program ID: {dr[0]} | Program: {dr[1]} | Bağlı Bölüm ID: {dr[2]}");
                }
            }
            catch (Exception ex) { Console.WriteLine("[-] Hata: " + ex.Message); }
            finally { conn.Close(); }
            Console.ReadKey();
        }
    }
}