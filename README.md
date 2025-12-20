Spor Salonu Yönetim ve Randevu Sistemi  
ASP.NET Core MVC – Web Programlama Proje Ödevi (2025–2026 Güz Dönemi)

Proje Amacı
Bu projenin amacı, Web Programlama dersinde teorik ve pratik olarak öğrenilen bilgilerin, gerçek hayat senaryosuna uygun bir web uygulaması üzerinden uygulanmasıdır.  
Bu kapsamda ASP.NET Core MVC kullanılarak bir Spor Salonu (Fitness Center) Yönetim ve Randevu Sistemi geliştirilmiştir.

Sistem; spor salonu hizmetlerini, antrenörleri, üyeleri, randevuları ve yapay zekâ destekli kişisel sağlık önerilerini merkezi bir yapı üzerinden yönetmeyi hedefler
 Genel Mimari
Proje katmanlı mimari yaklaşımıyla tasarlanmıştır.
Web_API
- Veritabanı ile iletişim yalnızca REST API üzerinden sağlanmaktadır  
- Tüm CRUD işlemleri API aracılığıyla gerçekleştirilmektedir  
- LINQ sorguları kullanılarak filtreleme ve veri işleme yapılmaktadır  
Web_Project (Client / MVC)
- Kullanıcı arayüzünü temsil eder  
- Web_API ile HTTP üzerinden haberleşir  
- Rol bazlı yetkilendirme ile dinamik menü ve sayfa yapısı sunar  

---

Veritabanı
- SQL Server kullanılmıştır  
- Entity Framework Core (Code First) yaklaşımı tercih edilmiştir  
- ASP.NET Core Identity ile kullanıcı ve rol yönetimi sağlanmıştır  

Başlıca tablolar:
- Members  
- Trainers  
- TrainerSkills  
- TrainerAvailabilities  
- Services  
- Appointments  
- AspNetUsers  
- AspNetRoles  
- AspNetUserRoles  
Kimlik Doğrulama ve Yetkilendirme
Sistemde rol bazlı yetkilendirme (Authorization) tüm seviyelerde uygulanmıştır.

 Üye (Member)
- Randevu alma  
- Kendi randevularını görüntüleme  
- Antrenör ve hizmetleri inceleme  

 Antrenör (Trainer)
- Sadece Trainer Dashboard erişimi  
- Kendi randevularını ve uygunluk saatlerini yönetme  

Admin
- Sadece Admin Panel erişimi  
- Sistem genelinde tüm CRUD işlemleri  

Yetkisi olmayan kullanıcılar, ilgili sayfaları menüde dahi göremez.

---

Yapay Zekâ Entegrasyonu
Projede yapay zekâ destekli sağlık ve egzersiz öneri modülü bulunmaktadır.

Kullanıcılar:
- Sadece fotoğraf yükleyerek  
- Sadece boy / kilo / yaş bilgisi girerek  
- Fotoğraf ve fiziksel bilgileri birlikte kullanarak  

kendilerine özel:
- Egzersiz ve diyet planı  
- Daha sağlıklı yaşam önerileri  
- Plan uygulandığında oluşabilecek gelecekteki görünümü temsil eden görsel  

elde edebilmektedir.

Bu kapsamda, görsel üretimi için **Banana** platformu kullanılmıştır.  
Banana, model barındırma ve yapay zekâ servisleri sunan **ücretli (paid) bir üçüncü taraf servisidir**.  
Görsel üretim işlemleri bu servis üzerinden API aracılığıyla gerçekleştirilmiştir.

Kullanılan Teknolojiler
- ASP.NET Core MVC  
- ASP.NET Core Web API  
- C#  
- SQL Server  
- Entity Framework Core  
- LINQ   
- HTML5  
- CSS3  
- JavaScript  
- Banana (AI görsel üretimi – ücretli servis)



Ad Soyad: Khaled Abdullatif
Ders: Web Programlama  
Ders Grubu: 2. Öğretim A
