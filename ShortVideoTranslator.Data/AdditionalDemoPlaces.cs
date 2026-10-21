using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

// Explicit additions for the version 7 migration; no existing record is replaced.
internal static class AdditionalDemoPlaces
{
    public static readonly Place[] All = [
        new Place(6, "Nhà thờ Đức Bà Sài Gòn", "Kiến trúc",
            DemoNarrations.For("Nhà thờ Đức Bà Sài Gòn").Texts["vi"],
            Address: "1 Công xã Paris, TP.HCM (khu vực Quận 1)",
            Latitude: 10.7797222222, Longitude: 106.6991666667, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Nhà thờ Đức Bà Sài Gòn").SourceUrl,
            AddressSourceUrl: "https://vietnamtourism.vn/index.php/tourism/items/270/3",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Notre-Dame_Cathedral_Basilica_of_Saigon",
            ImageUrl: "https://thumb.wikimedia.org/wikipedia/commons/thumb/0/01/Saigon%27s_Notre_Dame_Cathedral_%2823141452302%29.jpg/1280px-Saigon%27s_Notre_Dame_Cathedral_%2823141452302%29.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:Saigon%27s_Notre_Dame_Cathedral_(23141452302).jpg",
            ImageNote: "Góc bên Nhà thờ Đức Bà Sài Gòn · ảnh năm 2015, không phải hiện trạng — evelynquek / Wikimedia Commons · CC BY 2.0. Cắt khung khi hiển thị.", ImageFocusY: 40,
            Images: [new PlacePhoto(
                "https://thumb.wikimedia.org/wikipedia/commons/thumb/0/0b/2023-12-10_Notre-Dame_Cathedral_Basilica_of_Saigon_%28Nh%C3%A0_th%E1%BB%9D_ch%C3%ADnh_t%C3%B2a_%C4%90%E1%BB%A9c_B%C3%A0_S%C3%A0i_G%C3%B2n%29_01.jpg/1280px-2023-12-10_Notre-Dame_Cathedral_Basilica_of_Saigon_%28Nh%C3%A0_th%E1%BB%9D_ch%C3%ADnh_t%C3%B2a_%C4%90%E1%BB%A9c_B%C3%A0_S%C3%A0i_G%C3%B2n%29_01.jpg",
                "Mặt tiền đang trùng tu · ảnh tháng 12/2023, không khẳng định hiện trạng",
                "源義信 / Wikimedia Commons · CC BY-SA 4.0. Cắt khung khi hiển thị.",
                "https://commons.wikimedia.org/wiki/File:2023-12-10_Notre-Dame_Cathedral_Basilica_of_Saigon_(Nh%C3%A0_th%E1%BB%9D_ch%C3%ADnh_t%C3%B2a_%C4%90%E1%BB%A9c_B%C3%A0_S%C3%A0i_G%C3%B2n)_01.jpg", 40)]),
        new Place(7, "Bảo tàng Mỹ thuật TP.HCM", "Văn hóa & nghệ thuật",
            DemoNarrations.For("Bảo tàng Mỹ thuật TP.HCM").Texts["vi"],
            Address: "97A Phó Đức Chính, TP.HCM (khu vực Quận 1)",
            Latitude: 10.7694444444, Longitude: 106.7000000000, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Bảo tàng Mỹ thuật TP.HCM").SourceUrl,
            AddressSourceUrl: "https://www.baotangmythuattphcm.com.vn/tham-quan",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Ho_Chi_Minh_City_Museum_of_Fine_Arts",
            ImageUrl: "https://thumb.wikimedia.org/wikipedia/commons/thumb/a/af/B%E1%BA%A3o_t%C3%A0ng_M%E1%BB%B9_thu%E1%BA%ADt_Tp_%28ki%E1%BA%BFn_tr%C3%BAc_t%E1%BB%95ng_th%E1%BB%83%29_%281%29.jpg/1280px-B%E1%BA%A3o_t%C3%A0ng_M%E1%BB%B9_thu%E1%BA%ADt_Tp_%28ki%E1%BA%BFn_tr%C3%BAc_t%E1%BB%95ng_th%E1%BB%83%29_%281%29.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:B%E1%BA%A3o_t%C3%A0ng_M%E1%BB%B9_thu%E1%BA%ADt_Tp_(ki%E1%BA%BFn_tr%C3%BAc_t%E1%BB%95ng_th%E1%BB%83)_(1).jpg",
            ImageNote: "Mặt tiền Bảo tàng Mỹ thuật TP.HCM · ảnh năm 2022 — Phương Huy / Wikimedia Commons · CC BY-SA 4.0. Cắt khung khi hiển thị.", ImageFocusY: 48,
            Images: [new PlacePhoto(
                "https://thumb.wikimedia.org/wikipedia/commons/thumb/c/cb/B%E1%BA%A3o_t%C3%A0ng_M%E1%BB%B9_thu%E1%BA%ADt_Tp_%28ki%E1%BA%BFn_tr%C3%BAc_n%E1%BB%99i_th%E1%BA%A5t%2C_kh%C3%B4ng_gian_tr%C6%B0ng_b%C3%A0y_t%C3%A1c_ph%E1%BA%A9m%29.jpg/1280px-B%E1%BA%A3o_t%C3%A0ng_M%E1%BB%B9_thu%E1%BA%ADt_Tp_%28ki%E1%BA%BFn_tr%C3%BAc_n%E1%BB%99i_th%E1%BA%A5t%2C_kh%C3%B4ng_gian_tr%C6%B0ng_b%C3%A0y_t%C3%A1c_ph%E1%BA%A9m%29.jpg",
                "Góc trưng bày và không gian nội thất · ảnh năm 2022",
                "Phương Huy / Wikimedia Commons · CC BY-SA 4.0. Cắt khung khi hiển thị.",
                "https://commons.wikimedia.org/wiki/File:B%E1%BA%A3o_t%C3%A0ng_M%E1%BB%B9_thu%E1%BA%ADt_Tp_(ki%E1%BA%BFn_tr%C3%BAc_n%E1%BB%99i_th%E1%BA%A5t,_kh%C3%B4ng_gian_tr%C6%B0ng_b%C3%A0y_t%C3%A1c_ph%E1%BA%A9m).jpg", 65)]),
        new Place(8, "Công viên Tao Đàn", "Không gian công cộng",
            DemoNarrations.For("Công viên Tao Đàn").Texts["vi"],
            Address: "55C Nguyễn Thị Minh Khai, TP.HCM (khu vực Quận 1)",
            Latitude: 10.7747222222, Longitude: 106.6930555556, LocationVerified: true,
            SourceUrl: DemoNarrations.For("Công viên Tao Đàn").SourceUrl,
            AddressSourceUrl: "https://visithcmc.net/page/ke-hoach-chuyen-di",
            LocationSourceUrl: "https://commons.wikimedia.org/wiki/Category:Tao_Dan_Park",
            ImageUrl: "https://thumb.wikimedia.org/wikipedia/commons/thumb/8/8b/2023-12-10_Tao_%C4%90%C3%A0n_Park%2C_Saigon_01.jpg/1280px-2023-12-10_Tao_%C4%90%C3%A0n_Park%2C_Saigon_01.jpg",
            ImageSourceUrl: "https://commons.wikimedia.org/wiki/File:2023-12-10_Tao_%C4%90%C3%A0n_Park,_Saigon_01.jpg",
            ImageNote: "Một lối vào Công viên Tao Đàn · ảnh tháng 12/2023 — 源義信 / Wikimedia Commons · CC BY 4.0. Cắt khung khi hiển thị.", ImageFocusY: 36,
            Images: [new PlacePhoto(
                "https://thumb.wikimedia.org/wikipedia/commons/thumb/7/78/Tao_Dan_Park.jpg/1280px-Tao_Dan_Park.jpg",
                "Không gian cây xanh Công viên Tao Đàn · ảnh tháng 01/2024",
                "Vano111ru / Wikimedia Commons · CC BY-SA 4.0. Cắt khung khi hiển thị.",
                "https://commons.wikimedia.org/wiki/File:Tao_Dan_Park.jpg", 60)]),
    ];
}
