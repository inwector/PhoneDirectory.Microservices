# Telefon Rehberi Uygulaması / Phone Directory Application

Microservice mimarisi ile geliştirilmiş telefon rehberi uygulaması. Asenkron rapor üretimi ve Kafka tabanlı mesajlaşma sistemi içerir.

A phone directory application built with microservice architecture, featuring asynchronous report generation and Kafka-based messaging system.

## Proje Açıklaması / Project Description

Bu proje, birbirleri ile haberleşen minimum iki microservice'in olduğu bir yapıda tasarlanmış basit bir telefon rehberi uygulamasıdır. Sistem kişi yönetimi ve raporlama işlevlerini asenkron olarak gerçekleştirir.

This project is a simple phone directory application designed with a structure of at least two microservices that communicate with each other. The system performs contact management and reporting functions asynchronously.

## Özellikler / Features

### Temel İşlevler / Core Functions
- Rehberde kişi oluşturma / Create contact in directory
- Rehberden kişi silme / Remove contact from directory  
- Rehberdeki kişiye iletişim bilgisi ekleme / Add contact information to person
- Rehberdeki kişiden iletişim bilgisini silme / Remove contact information from person
- Rehberdeki kişilerin listelenmesi / List contacts in directory
- Kişi detay bilgilerinin getirilmesi / Get detailed contact information

### Raporlama / Reporting
- Konuma göre istatistik raporu talebi / Request location-based statistics report
- Sistem raporlarının listelenmesi / List system reports
- Rapor detay bilgilerinin getirilmesi / Get report details

## Teknik Mimari / Technical Architecture

### Mikroservisler / Microservices
- **Kişi Servisi / Contact Service**: Kişi ve iletişim bilgisi yönetimi
- **Rapor Servisi / Report Service**: Asenkron rapor üretimi ve yönetimi

### Teknolojiler / Technologies
- **Backend**: .NET Core
- **Veritabanı / Database**: PostgreSQL
- **Mesaj Kuyruğu / Message Queue**: Apache Kafka
- **Konteynerleştirme / Containerization**: Docker & Docker Compose

## Veri Yapısı / Data Structure

### Kişi / Contact
- UUID
- Ad / First Name
- Soyad / Last Name
- Firma / Company
- İletişim Bilgileri / Contact Information
  - Bilgi Tipi / Info Type: Telefon Numarası, E-mail Adresi, Konum
  - Bilgi İçeriği / Info Content

### Rapor / Report
- UUID
- Raporun Talep Edildiği Tarih / Report Request Date
- Rapor Durumu / Report Status: (Hazırlanıyor/Preparing, Tamamlandı/Completed)
- Rapor İçeriği / Report Content:
  - Konum Bilgisi / Location Information
  - O konumda kayıtlı kişi sayısı / Number of registered contacts in that location
  - O konumda kayıtlı telefon numarası sayısı / Number of registered phone numbers in that location

## Kurulum ve Çalıştırma / Installation and Running

### Gereksinimler / Prerequisites
- .NET 8.0 SDK
- Docker & Docker Compose
- Visual Studio veya VS Code

### 1. Proje Kurulumu / Project Setup
```bash
git clone [repository-url]
cd phone-directory
```

### 2. Docker Servislerini Başlatma / Start Docker Services
```bash
docker-compose up -d
```

Bu komut şunları başlatır / This starts:
- PostgreSQL veritabanı / PostgreSQL database
- Apache Kafka
- Gerekli Kafka topic'leri otomatik oluşturulur / Required Kafka topics auto-created

### 3. Veritabanı Migrasyonu / Database Migration
```bash
dotnet ef database update
```

### 4. Uygulamayı Çalıştırma / Run Application
Visual Studio'da **Multiple Startup Projects** olarak ayarlayın ve çalıştırın.
Set as **Multiple Startup Projects** in Visual Studio and run.

Manuel çalıştırma / Manual run:
```bash
dotnet run --project ContactService
dotnet run --project ReportService
```

## API Uç Noktaları / API Endpoints

### Kişi Yönetimi / Contact Management
- `GET /api/persons` - Tüm kişileri listele / List all persons
- `GET /api/persons/{id}` - Kişi detayını getir / Get person details
- `POST /api/persons` - Yeni kişi oluştur / Create new person
- `DELETE /api/persons/{id}` - Kişiyi sil / Delete person

### İletişim Bilgisi Yönetimi / Persons Information Management  
- `POST /api/persons/{personId}/contactinfos` - İletişim bilgisi ekle / Add persons info
- `GET /api/persons/{personId}/contactinfos/{id}` - Kişi hakkında iletişim bilgisini listele / Get info about a person's detail
- `DELETE /api/persons/{personId}/contactinfos/{id}` - İletişim bilgisi sil / Remove persons info

### Rapor Yönetimi / Report Management
- `POST /api/reports` - Konum raporu talep et / Request location report
- `GET /api/reports` - Tüm raporları listele / List all reports  
- `GET /api/reports/{id}` - Rapor detayını getir / Get report details

## Yapılandırma / Configuration

### Veritabanı / Database
`appsettings.json` dosyasında PostgreSQL bağlantı ayarları:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=PhoneDirectoryDB;Username=postgres;Password=yourpassword"
  }
}
```

### Kafka Ayarları / Kafka Settings
- **Bootstrap Servers**: `localhost:9092`
- **Topics**: 
  - `report-requests` - Rapor talepleri için
  - `report-results` - Rapor sonuçları için

## Test Çalıştırma / Running Tests

Visual Studio'da testlere sağ tıklayıp "Run Tests" seçin.
Right-click on tests in Visual Studio and select "Run Tests".

Komut satırından / Via command line:
```bash
dotnet test
```

Test kapsamı minimum %60 olacak şekilde yapılandırılmıştır.
Test coverage is configured to be minimum 60%.

## Asenkron Rapor Sistemi / Asynchronous Report System

### Çalışma Mantığı / Working Logic
1. Kullanıcı `/api/reports` endpoint'ine rapor talebi gönderir
2. Sistem raporu "Hazırlanıyor" durumunda oluşturur
3. Kafka üzerinden rapor talebi mesajı gönderilir
4. Rapor servisi mesajı alır ve raporu arka planda türetir
5. Rapor tamamlandığında durum "Tamamlandı" olarak güncellenir
6. Kullanıcı `/api/reports` endpoint'i ile rapor durumunu kontrol edebilir

### Message Flow
1. User sends report request to `/api/reports` endpoint
2. System creates report with "Preparing" status and returns UUID
3. Report request message is sent via Kafka
4. Report service receives message and generates report in background
5. When report is completed, status is updated to "Completed"
6. User can check report status via `/api/reports` endpoint

## Kafka Konfigürasyonu / Kafka Configuration

### Consumer Ayarları / Consumer Settings
- **ReportRequestConsumer**: `report-requests` topic'ini dinler
- **ReportResultConsumer**: `report-results` topic'ini dinler
- **Consumer Groups**: 
  - `report-service-group`
  - `report-result-service-group`

## Git Workflow

Proje development branch'i ve master branch'i kullanır:
- `development` - Geliştirme branch'i
- `master` - Ana branch
- Sürüm tagları kullanılır

Project uses development and master branches:
- `development` - Development branch  
- `master` - Main branch
- Version tags are used

## Docker Servisleri / Docker Services

### Kafka
- Kafka Port: 9092

## Önemli Notlar / Important Notes

- Tüm Kafka topic'leri otomatik oluşturulur / All Kafka topics are auto-created
- Rapor üretimi asenkron olarak çalışır / Report generation works asynchronously  
- Sistem darboğaz yaratmadan sıralı rapor işleme yapar / System processes reports sequentially without bottleneck
- Docker çalıştığından emin olun / Ensure Docker is running

## İletişim

Proje ile ilgili sorularınızı izzet.buyukkurkcu@gmail.com adresine iletebilirsiniz.

## Lisans

Apache 2.0 ile lisanslanmıştır.