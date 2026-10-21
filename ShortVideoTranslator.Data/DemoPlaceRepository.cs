using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

// Bộ dữ liệu demo Quận 1: thuyết minh biên soạn riêng, địa chỉ và ảnh có nguồn đối chiếu.
// Tọa độ chuyển đổi từ DMS trên trang Commons; vị trí tham khảo, không phải cổng vào đo GPS.
public class DemoPlaceRepository : IPlaceRepository
{
    private static readonly IReadOnlyList<Place> Places = Array.AsReadOnly(new[]
    {
        new Place(1, "Chợ Bến Thành", "Chợ truyền thống",
            DemoNarrations.For("Chợ Bến Thành").Texts["vi"],
            Address: "Cửa Nam, đường Lê Lợi, TP.HCM (khu vực Quận 1)",
            Latitude: 10.7725222222, Longitude: 106.6980194444, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Chợ Bến Thành").SourceUrl,
            AddressSourceUrl: "https://www.congbao.hochiminhcity.gov.vn/tin-tuc-tong-hop/uy-ban-nhan-dan-thanh-pho-ho-chi-minh-ban-hanh-quyet-%C4%91inh-so-5300-q%C4%91-ubnd-ve-viec-xep-hang-di-tich-cap-thanh-pho-%C4%91oi-voi-cho-ben-thanh",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Ben_Thanh_Market",
            ImageUrl: "https://upload.wikimedia.org/wikipedia/commons/9/91/Ben_Thanh_market_2.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:Ben_Thanh_market_2.jpg",
            ImageNote: "Chợ Bến Thành — Riza / Wikimedia Commons, CC BY 2.0. Ảnh được cắt khung khi hiển thị.", ImageFocusY: 35),
        new Place(2, "Dinh Độc Lập", "Di tích lịch sử",
            DemoNarrations.For("Dinh Độc Lập").Texts["vi"],
            Address: "135 Nam Kỳ Khởi Nghĩa, phường Bến Thành, TP.HCM (khu vực Quận 1)",
            Latitude: 10.77705, Longitude: 106.6953694444, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Dinh Độc Lập").SourceUrl,
            AddressSourceUrl: "https://dinhdoclap.gov.vn/so-do-dinh-doc-lap/",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Independence_Palace",
            ImageUrl: "https://upload.wikimedia.org/wikipedia/commons/1/18/2019-09-13_Dinh_Th%E1%BB%91ng_Nh%E1%BA%A5t.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:2019-09-13_Dinh_Th%E1%BB%91ng_Nh%E1%BA%A5t.jpg",
            ImageNote: "Dinh Độc Lập — LWYang / Wikimedia Commons, CC BY 2.0. Ảnh được cắt khung khi hiển thị.", ImageFocusY: 50),
        new Place(3, "Bưu điện Trung tâm Sài Gòn", "Kiến trúc",
            DemoNarrations.For("Bưu điện Trung tâm Sài Gòn").Texts["vi"],
            Address: "2 Công xã Paris, TP.HCM (khu vực Quận 1)",
            Latitude: 10.78, Longitude: 106.7, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Bưu điện Trung tâm Sài Gòn").SourceUrl,
            AddressSourceUrl: "https://www.lonelyplanet.com/points-of-interest/central-post-office/1410705",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Saigon_Central_Post_Office",
            ImageUrl: "https://upload.wikimedia.org/wikipedia/commons/thumb/3/31/Interior_of_the_Saigon_Central_Post_Office_2014.jpg/1280px-Interior_of_the_Saigon_Central_Post_Office_2014.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:Interior_of_the_Saigon_Central_Post_Office_2014.jpg",
            ImageNote: "Không gian bên trong Bưu điện Trung tâm Sài Gòn, ảnh năm 2014 — Ekrem Canli / Wikimedia Commons, CC BY-SA 3.0. Cắt khung khi hiển thị.", ImageFocusY: 50),
        new Place(4, "Phố đi bộ Nguyễn Huệ", "Không gian công cộng",
            DemoNarrations.For("Phố đi bộ Nguyễn Huệ").Texts["vi"],
            Address: "Đường Nguyễn Huệ, đoạn Lê Lợi – Tôn Đức Thắng, TP.HCM (khu vực Quận 1)",
            Latitude: 10.7741111111, Longitude: 106.7036194444, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Phố đi bộ Nguyễn Huệ").SourceUrl,
            AddressSourceUrl: "https://vietnamtourism.vn/index.php/tourism/items/2905/2",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Nguyen_Hue_Boulevard,_Ho_Chi_Minh_City",
            ImageUrl: "https://upload.wikimedia.org/wikipedia/commons/1/13/Liberty_Central_Saigon_Citypoint_3.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:Liberty_Central_Saigon_Citypoint_3.jpg",
            ImageNote: "Khu vực tượng đài trên phố Nguyễn Huệ, chụp 26/02/2025 — Andre Hospers / Wikimedia Commons, CC BY-SA 4.0. Cắt khung khi hiển thị.", ImageFocusY: 55),
        new Place(5, "Nhà hát Thành phố", "Văn hóa & nghệ thuật",
            DemoNarrations.For("Nhà hát Thành phố").Texts["vi"],
            Address: "7 Công trường Lam Sơn, TP.HCM (khu vực Quận 1)",
            Latitude: 10.7763888889, Longitude: 106.7027805556, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Nhà hát Thành phố").SourceUrl,
            AddressSourceUrl: "https://hbso.org.vn/chuong-trinh/dem-nhac-debussy-ravel-973.html",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Municipal_Theatre_of_Ho_Chi_Minh_City",
            ImageUrl: "https://upload.wikimedia.org/wikipedia/commons/6/60/HCM-City_Oper.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:HCM-City_Oper.jpg",
            ImageNote: "Nhà hát Thành phố — Andre Lettau / Wikimedia Commons, CC BY-SA 3.0. Ảnh năm 2003, được cắt khung khi hiển thị.", ImageFocusY: 35)
    }.Concat(AdditionalDemoPlaces.All).Select(p => p with { Images = p.Images ?? DemoPlacePhotos.For(p.Name) }).ToArray());

    public IReadOnlyList<Place> GetAll() => Places;
    public Place? GetById(int id) => Places.FirstOrDefault(place => place.Id == id);
}
