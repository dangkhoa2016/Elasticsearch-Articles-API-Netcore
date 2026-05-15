# Tài Liệu Dự Án Kiểm Thử

> 🌐 Language / Ngôn ngữ: [English](README.md) | **Tiếng Việt**

## Tổng Quan

Dự án `ElasticsearchArticlesApiNetcore.Tests` cung cấp kiểm thử đơn vị (unit test) và kiểm thử tích hợp (integration test) cho API Quản lý Bài viết với Elasticsearch. Sử dụng xUnit làm khung kiểm thử, Moq để mock đối tượng, FluentAssertions cho cú pháp assertion dễ đọc, và `Microsoft.AspNetCore.Mvc.Testing` cho kiểm thử tích hợp.

## Yêu Cầu Trước Khi Chạy (Prerequisites)

Để đảm bảo các bài kiểm thử (đặc biệt là kiểm thử tích hợp) chạy thành công, máy máy tính của bạn cần đáp ứng các điều kiện sau:

1. **.NET SDK:** Phiên bản 10.0 trở lên.
2. **Docker:** Cần thiết cho kiểm thử tích hợp với Elasticsearch.
* Trước khi chạy test, hãy đảm bảo instance Elasticsearch dành cho môi trường test đã được khởi động:
```bash
docker run -d --name es-test -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" elasticsearch:8.11.0

```


3. **SQLite:** Không cần cài đặt cấu hình vì dự án sử dụng SQLite In-Memory (tự động khởi tạo trong bộ nhớ khi chạy).

---

## Cấu Trúc Thư Mục

```
ElasticsearchArticlesApiNetcore.Tests/
├── ElasticsearchArticlesApiNetcore.Tests.csproj  # Cấu hình dự án kiểm thử
├── appsettings.Testing.json                       # Cấu hình môi trường kiểm thử
├── TestBase.cs                                    # Lớp cơ sở cho kiểm thử tích hợp
├── TestProjectSetupTests.cs                       # Xác nhận hạ tầng kiểm thử hoạt động
├── Services/                                      # Kiểm thử đơn vị cho lớp service
│   └── (ArticleServiceTests.cs, v.v.)
├── Validators/                                    # Kiểm thử đơn vị cho FluentValidation validators
│   └── (ArticleViewModelValidatorTests.cs, v.v.)
└── Integration/                                   # Kiểm thử tích hợp cho API
    └── (ArticlesApiTests.cs, DatabaseTests.cs, v.v.)

```

---

## Công Nghệ Sử Dụng

| Package | Phiên bản | Mục đích |
| --- | --- | --- |
| xUnit | 2.4.2 | Khung kiểm thử |
| Moq | 4.20.72 | Khung mock đối tượng |
| FluentAssertions | 8.2.0 | Cú pháp assertion dễ đọc |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.8 | Kiểm thử tích hợp với WebApplicationFactory |
| Microsoft.NET.Test.Sdk | 17.6.0 | Bộ công cụ kiểm thử |
| coverlet.collector | 6.0.0 | Thu thập độ phủ mã nguồn |

---

## Chạy Kiểm Thử

Trước khi chạy, hãy đảm bảo biến môi trường .NET đã được thiết lập chính xác trên terminal của bạn:

```bash
export PATH="$HOME/.dotnet:$PATH"

```

### 1. Lệnh chạy cơ bản

* **Chạy tất cả kiểm thử:**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj

```

* **Chạy với đầu ra chi tiết (Xem trực tiếp trên terminal):**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj --verbosity normal

```

* **Chạy một lớp kiểm thử cụ thể:**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj --filter "FullyQualifiedName~TestProjectSetupTests"

```

* **Chạy với thống kê độ phủ mã nguồn:**

```bash
  dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj --collect:"XPlat Code Coverage"

```

### 2. Xuất Log ra file (Dành cho CI/CD hoặc lưu vết)

Khi chuyển hướng log (redirect) ra file, bạn cần tắt tính năng biên dịch chia sẻ (`UseSharedCompilation=false`) để tránh việc MSBuild giữ lại bộ đệm (buffer) làm trống/mất log.

* **Lệnh xuất toàn bộ log ra file `logs/test.log` (Ghi nhận real-time):**

```bash
  mkdir -p logs && dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj /p:UseSharedCompilation=false > logs/test.log 2>&1

```

* **Xem log thay đổi theo thời gian thực tại terminal khác:**

```bash
  tail -f logs/test.log

```

### 3. Chạy tuần tự (Tránh xung đột dữ liệu)

Mặc định xUnit sẽ chạy song song các lớp test. Nếu các bài kiểm thử tích hợp (Integration Test) can thiệp chung vào một Index Elasticsearch hoặc tạo/xóa dữ liệu SQLite gây lỗi chéo nhau, hãy ép chạy tuần tự bằng lệnh:

```bash
dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj -- .CollectionBehavior.MaxParallelThreads=1

```

---

## Cấu Hình Kiểm Thử

### appsettings.Testing.json

Môi trường kiểm thử sử dụng cấu hình riêng, tối ưu cho việc kiểm thử và hoàn toàn cô lập với Production:

| Thiết lập | Giá trị | Lý do |
| --- | --- | --- |
| Database | `Data Source=:memory:` | SQLite trong bộ nhớ, kiểm thử nhanh và cô lập |
| Rate Limiting | Vô hiệu hóa | Tránh lỗi kiểm thử do giới hạn tốc độ |
| Elasticsearch Index | `articles-test` | Index riêng, không ảnh hưởng dữ liệu production |
| JWT Secret | Khóa kiểm thử | Tách biệt với thông tin xác thực production |
| Hết hạn Cache | 1 phút | Cache xoay vòng nhanh cho các tình huống kiểm thử |

---

## Viết Kiểm Thử

### Ví dụ Kiểm Thử Đơn Vị (Unit Test)

```csharp
using Moq;
using FluentAssertions;

namespace ElasticsearchArticlesApiNetcore.Tests.Services;

public class ArticleServiceTests
{
    [Fact]
    public async Task GetArticles_ShouldReturnPagedResults()
    {
        // Sắp xếp (Arrange)
        var mockRepository = new Mock<IArticleRepository>();
        mockRepository.Setup(r => r.GetArticlesAsync(1, 10))
            .ReturnsAsync(new { articles = new List<Article>(), total = 50 });

        // Thực thi (Act)
        var result = await mockRepository.Object.GetArticlesAsync(1, 10);

        // Xác nhận (Assert)
        result.total.Should().Be(50);
        mockRepository.Verify(r => r.GetArticlesAsync(1, 10), Times.Once);
    }
}

```

### Ví dụ Kiểm Thử Tích Hợp (Integration Test)

```csharp
using System.Net;
using FluentAssertions;

namespace ElasticsearchArticlesApiNetcore.Tests.Integration;

public class ArticlesApiTests : TestBase
{
    public ArticlesApiTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetArticles_ShouldReturnOk()
    {
        // Thực thi
        var response = await _client.GetAsync("/api/articles");

        // Xác nhận
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

```

---

## Lớp TestBase

Lớp `TestBase` cung cấp `WebApplicationFactory<Program>` đã được cấu hình sẵn cho kiểm thử tích hợp:

```csharp
public abstract class TestBase : IClassFixture<TestBase.TestWebApplicationFactory>
{
    protected readonly HttpClient _client;
    protected readonly TestWebApplicationFactory _factory;

    protected TestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected IServiceScope CreateScope()
        => _factory.Services.CreateScope();
}

```

> 💡 **Cơ chế hoạt động:** Lớp này tự động cấu hình môi trường sang `Testing`, tự động chạy các tệp Migration để dựng cấu hình bảng dữ liệu trên SQLite In-Memory mới hoàn toàn cho mỗi phiên chạy, và dọn dẹp sạch sẽ sau khi test xong.

---

## Thực Hành Tốt Nhất (Best Practices)

1. **Cô lập kiểm thử**: Mỗi kiểm thử phải độc lập. Sử dụng mock cho các phụ thuộc bên ngoài trong phạm vi Unit Test.
2. **Đặt tên mô tả**: Tên phương thức kiểm thử nên mô tả tình huống và kết quả mong đợi (Ví dụ: `MethodName_StateUnderTest_ExpectedBehavior`).
3. **Arrange-Act-Assert**: Luôn cấu trúc mã nguồn kiểm thử với 3 phần rõ ràng: sắp xếp, thực thi, xác nhận.
4. **Sử dụng FluentAssertions**: Ưu tiên `value.Should().Be(expected)` thay vì `Assert.Equal(expected, value)` để câu thông báo lỗi tự nhiên và dễ đọc hơn khi test fail.
5. **Dọn dẹp tài nguyên**: Sử dụng `IClassFixture` cho các thiết lập nặng tốn thời gian dùng chung (như khởi tạo WebApplicationFactory) và `IDisposable` nếu cần dọn dẹp dữ liệu rác sau mỗi test case.
