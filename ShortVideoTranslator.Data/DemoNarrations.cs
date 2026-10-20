using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Data;

// Independently written demo scripts. Fact/source mapping: docs/NARRATION-CONTENT.md.
// Foreign-language drafts are AI-assisted, not professionally reviewed translations.
public sealed record DemoNarration(string Name, string PreviousText, string SourceUrl, Dictionary<string, string> Texts);
public sealed class DemoNarrations : ISampleNarrationCatalog
{
    public const string Provider = "SmartTourDraft";
    public static readonly IReadOnlyList<DemoNarration> All = Array.AsReadOnly(new[] {
        new DemoNarration("Chợ Bến Thành",
            "Chợ Bến Thành nằm ở trung tâm Quận 1, là điểm tham quan, mua sắm và khám phá ẩm thực Sài Gòn.",
            "https://benthanhmarket.vn/about/gioi-thieu-ve-cho-ben-thanh.html", new() {
            ["vi"] = """
                Chào mừng bạn đến với Chợ Bến Thành, một điểm dừng quen thuộc trong hành trình khám phá trung tâm Sài Gòn. Hãy bắt đầu bằng việc tìm tháp đồng hồ ở cửa Nam. Chi tiết này giúp chúng ta nhận ra ngôi chợ giữa nhịp phố đông đúc.

                Chợ tại vị trí hiện nay được khánh thành vào năm 1914. Trước đó, chợ Bến Thành từng nằm ở khu vực gần bến sông. Sự dịch chuyển ấy gợi lại quá trình thành phố lớn lên từ hoạt động giao thương đường thủy. Bên trong chợ là nhiều gian hàng, từ hàng thủ công, quà lưu niệm đến thực phẩm và món ăn.

                Khi tham quan, bạn có thể đi chậm qua các lối hàng, quan sát màu sắc và cách người bán giới thiệu sản phẩm. Nếu muốn mua sắm hoặc thưởng thức món ăn, hãy hỏi rõ giá và khẩu phần trước khi chọn. Một món quà nhỏ, một cuộc trò chuyện lịch sự hay một hương vị mới đều có thể trở thành kỷ niệm của chuyến đi. Hãy giữ lối đi thông thoáng để việc tham quan và buôn bán cùng diễn ra thuận tiện.
                """,
            ["en"] = """
                Welcome to Ben Thanh Market, a familiar stop when exploring central Saigon. Start by looking for the clock tower above the southern entrance. It is a useful landmark amid the busy streets.

                The market at its present location opened in 1914. Earlier Ben Thanh markets stood near the river landing. This move recalls the city's growth through waterborne trade. Inside, stalls offer handicrafts, souvenirs, food products and prepared dishes.

                Walk slowly through the aisles and notice the colours and the way vendors present their goods. Before buying or ordering food, ask about prices and portion sizes. A small gift, a polite conversation or a new flavour can become a memorable part of your visit. Keep the aisles clear for other visitors and people working here.
                """,
            ["zh"] = """
                欢迎来到滨城市场，这里是探索西贡市中心的一个熟悉站点。先找找南门上方的钟楼吧。在繁忙的街区中，它是辨认市场的醒目标志。

                现址的市场于一九一四年启用。更早的滨城市场曾位于河边码头附近。这次迁移让人联想到城市随着水路贸易发展的历程。市场内有手工艺品、纪念品、食品和餐饮等摊位。

                不妨放慢脚步，观察摊位的色彩以及商贩介绍商品的方式。购买商品或点餐前，请先问清价格和份量。一件小礼物、一次礼貌的交谈或一种新口味，都可能成为旅途中的回忆。请保持通道畅通，方便游客通行和商贩经营。
                """,
            ["ja"] = """
                ベンタイン市場へようこそ。サイゴン中心部を巡る旅の立ち寄り先です。まず南側の入口にある時計塔を探してみましょう。にぎやかな街の中で、市場を見つける目印になります。

                現在の場所の市場は、一九一四年に開かれました。それ以前のベンタイン市場は、川の船着き場の近くにありました。この移転からは、水運による交易とともに発展した街の歩みを感じられます。市場には工芸品、お土産、食料品、料理などの店が並びます。

                通路をゆっくり歩き、商品の色彩や店の人の紹介の仕方に目を向けてみてください。買い物や注文の前には、値段と量を確認しましょう。小さなお土産、丁寧な会話、新しい味が旅の思い出になるかもしれません。見学や商いの妨げにならないよう、通路をふさがずに楽しみましょう。
                """,
            ["ko"] = """
                벤탄 시장에 오신 것을 환영합니다. 사이공 중심부를 둘러볼 때 들르기 좋은 장소입니다. 먼저 남쪽 입구 위의 시계탑을 찾아보세요. 복잡한 거리에서 시장을 알아보는 데 도움이 되는 표지입니다.

                현재 위치의 시장은 1914년에 문을 열었습니다. 이전의 벤탄 시장은 강가 선착장 근처에 있었습니다. 시장의 이동은 수상 교역과 함께 성장한 도시의 역사를 떠올리게 합니다. 시장 안에는 수공예품, 기념품, 식료품과 음식을 파는 가게들이 있습니다.

                통로를 천천히 걸으며 상품의 색감과 상인들이 물건을 소개하는 모습을 살펴보세요. 물건을 사거나 음식을 주문하기 전에 가격과 양을 확인하는 것이 좋습니다. 작은 선물, 정중한 대화, 새로운 맛이 여행의 추억이 될 수 있습니다. 방문객과 상인들이 편리하게 다닐 수 있도록 통로를 비워 주세요.
                """,
            ["fr"] = """
                Bienvenue au marché Ben Thanh, une étape familière pour découvrir le centre de Saigon. Commencez par repérer la tour de l'horloge au-dessus de l'entrée sud. Elle permet de reconnaître le marché au milieu des rues animées.

                Le marché à son emplacement actuel a ouvert en 1914. Les anciens marchés Ben Thanh se trouvaient près d'un embarcadère. Ce déplacement rappelle le développement de la ville autour du commerce fluvial. À l'intérieur, les étals proposent artisanat, souvenirs, produits alimentaires et plats préparés.

                Prenez le temps de parcourir les allées, d'observer les couleurs et la manière dont les commerçants présentent leurs produits. Avant un achat ou une commande, demandez le prix et la taille des portions. Un petit cadeau, une conversation courtoise ou une nouvelle saveur peuvent devenir un souvenir du voyage. Laissez les passages libres pour les visiteurs et les commerçants.
                """ }),
        new DemoNarration("Dinh Độc Lập",
            "Dinh Độc Lập là di tích lịch sử quốc gia đặc biệt, nằm tại số 135 Nam Kỳ Khởi Nghĩa, khu vực Quận 1.",
            "https://dinhdoclap.gov.vn/kien-truc-dinh-doc-lap/", new() {
            ["vi"] = """
                Điểm dừng tiếp theo là Dinh Độc Lập, tại số một trăm ba mươi lăm, đường Nam Kỳ Khởi Nghĩa. Đây là nơi bạn có thể tìm hiểu một dấu mốc lịch sử quan trọng, đồng thời quan sát một công trình kiến trúc tiêu biểu của thành phố.

                Tòa dinh hiện nay do kiến trúc sư Ngô Viết Thụ thiết kế, được khởi công năm 1962 và khánh thành năm 1966. Công trình kết hợp cách tổ chức không gian hiện đại với những ý tưởng từ văn hóa Á Đông. Khi nhìn mặt tiền, hãy chú ý lớp rèm hoa đá ở tầng hai, với hình dáng gợi những đốt trúc. Dinh cũng gắn với sự kiện ngày ba mươi tháng tư năm 1975 và được xếp hạng di tích quốc gia đặc biệt vào năm 2009.

                Trong chuyến tham quan, hãy dành thời gian đọc bảng giới thiệu tại từng khu vực, thay vì chỉ chụp ảnh mặt ngoài. Những phòng chức năng và không gian trưng bày sẽ giúp bạn đặt kiến trúc vào bối cảnh lịch sử. Hãy đi theo tuyến tham quan được hướng dẫn và giữ gìn hiện vật. Một nhịp bước chậm sẽ giúp chuyến đi trở thành dịp lắng nghe câu chuyện của thành phố.
                """,
            ["en"] = """
                Our next stop is Independence Palace, at 135 Nam Ky Khoi Nghia Street. Here you can explore an important historical landmark and a notable work of the city's architecture.

                Architect Ngo Viet Thu designed the present palace. Construction began in 1962 and it opened in 1966. The design combines modern spatial planning with ideas from East Asian culture. Look at the decorative stone screen on the second floor, whose shapes suggest bamboo stems. The palace is associated with the events of April 30, 1975, and was designated a special national historical site in 2009.

                During your visit, take time to read the information panels rather than only photographing the exterior. The rooms and displays help place the architecture in its historical context. Follow the indicated visitor route and respect the exhibits. A slower pace can turn this visit into a chance to listen to the city's story.
                """,
            ["zh"] = """
                下一站是位于南圻起义街一百三十五号的独立宫。在这里，您可以了解一处重要历史地标，也可以欣赏城市具有代表性的建筑。

                现存宫殿由建筑师吴曰树设计，一九六二年动工，一九六六年落成。设计将现代空间布局与东亚文化理念结合。请留意二层外立面的石制装饰屏，其造型让人联想到竹节。独立宫与一九七五年四月三十日的历史事件密切相关，并于二〇〇九年被列为国家特别历史遗迹。

                参观时，不妨花时间阅读各区域的说明牌，而不只是拍摄建筑外观。不同功能的房间和展陈能帮助您理解建筑的历史背景。请沿指定路线参观，并爱护展品。放慢脚步，也许能更好地倾听这座城市的故事。
                """,
            ["ja"] = """
                次の目的地は、ナムキーコイギア通り百三十五番地にある統一会堂です。独立宮とも呼ばれ、歴史の節目と、街を代表する建築の両方に触れられる場所です。

                現在の建物は建築家ゴー・ヴィエット・トゥーの設計で、一九六二年に着工し、一九六六年に完成しました。近代的な空間構成に、東アジアの文化に由来する考え方が取り入れられています。二階の外側にある石の装飾スクリーンには、竹の節を思わせる形が見られます。この場所は一九七五年四月三十日の出来事と結びつき、二〇〇九年に国家特別史跡に指定されました。

                外観の写真だけでなく、各所の説明板にも目を通してみましょう。部屋や展示を通して、建築の歴史的な背景が見えてきます。案内された順路を守り、展示物を大切にしてください。ゆっくり歩くことで、街の物語をより深く感じられるでしょう。
                """,
            ["ko"] = """
                다음 목적지는 남끼커이응이어 거리 135번지에 있는 독립궁입니다. 중요한 역사적 장소이자 도시의 대표적인 건축물을 함께 살펴볼 수 있는 곳입니다.

                현재의 건물은 건축가 응오비엣투가 설계했으며 1962년에 착공해 1966년에 완공되었습니다. 현대적인 공간 구성에 동아시아 문화에서 얻은 생각을 접목했습니다. 정면을 볼 때는 대나무 마디를 떠올리게 하는 2층의 장식용 돌 스크린에 주목해 보세요. 독립궁은 1975년 4월 30일의 역사적 사건과 연결되어 있으며, 2009년에 국가 특별 사적으로 지정되었습니다.

                외관 사진만 찍기보다 각 구역의 안내문을 읽어 보세요. 여러 방과 전시를 통해 건축의 역사적 맥락을 이해할 수 있습니다. 안내된 관람 동선을 따르고 전시물을 보호해 주세요. 천천히 걷는 시간이 도시의 이야기를 듣는 기회가 될 것입니다.
                """,
            ["fr"] = """
                Notre prochaine étape est le palais de l'Indépendance, au 135, rue Nam Ky Khoi Nghia. Vous pouvez y découvrir un lieu historique important et une œuvre remarquable de l'architecture de la ville.

                L'architecte Ngo Viet Thu a conçu le palais actuel. Sa construction a commencé en 1962 et il a été inauguré en 1966. Il associe une organisation moderne des espaces à des idées issues de la culture est-asiatique. Observez l'écran décoratif en pierre du deuxième étage : ses formes évoquent des tiges de bambou. Le palais est lié aux événements du 30 avril 1975 et a été classé site historique national spécial en 2009.

                Prenez le temps de lire les panneaux, au-delà des photographies de façade. Les salles et les expositions replacent l'architecture dans son contexte historique. Suivez le parcours indiqué et respectez les objets exposés. Une visite à pas tranquilles permet de mieux écouter l'histoire de la ville.
                """ }),
        new DemoNarration("Bưu điện Trung tâm Sài Gòn",
            "Bưu điện Trung tâm Sài Gòn nằm tại số 2 Công xã Paris, khu vực Quận 1, đối diện Nhà thờ Đức Bà.",
            "https://vnpost.vn/en/hoat-dong-nganh/buu-dien-trung-tam-thanh-pho-ho-chi-minh-mot-diem-den-hap-dan", new() {
            ["vi"] = """
                Chào mừng bạn đến với Bưu điện Trung tâm Sài Gòn, tại số hai Công xã Paris, gần Nhà thờ Đức Bà. Điểm dừng này đưa chúng ta về một thời, khi thư từ và điện tín là những sợi dây quan trọng nối con người qua khoảng cách.

                Tòa nhà được xây dựng trong giai đoạn từ năm 1886 đến năm 1891. Bên trong, phần mái vòm và không gian sảnh tạo nên một góc nhìn khác với mặt tiền bên ngoài. Theo giới thiệu của Bưu điện Việt Nam, công trình còn lưu giữ hai bản đồ lịch sử: một bản thể hiện Sài Gòn và vùng phụ cận năm 1892, bản còn lại thể hiện mạng lưới điện tín ở miền Nam Việt Nam và Campuchia năm 1936.

                Khi bước vào, hãy thử ngẩng nhìn kết cấu phía trên rồi dành một chút thời gian quan sát các bản đồ. Những đường nét ấy giúp hình dung cách người xưa kết nối và ghi nhận không gian sống. Bạn có thể xem chuyến ghé thăm như một cuộc gặp giữa nhịp liên lạc xưa và thói quen nhắn tin hôm nay. Hãy chụp ảnh ở vị trí phù hợp, giữ trật tự và tránh cản trở người đang giao dịch.
                """,
            ["en"] = """
                Welcome to Saigon Central Post Office, at 2 Cong Xa Paris, near Notre Dame Cathedral. This stop recalls a time when letters and telegrams were important links between people far apart.

                The building was constructed between 1886 and 1891. Inside, the vaulted ceiling and entrance hall offer a different perspective from the exterior. Vietnam Post describes two historic maps preserved here: one shows Saigon and its surroundings in 1892; the other shows the telegraph network of southern Vietnam and Cambodia in 1936.

                Look up at the structure, then take a moment to study the maps. They offer a way to imagine how earlier generations connected places and recorded their surroundings. Think of your visit as a meeting between older communication and today's instant messages. Choose a suitable place for photographs, keep your voice low and avoid obstructing people using postal services.
                """,
            ["zh"] = """
                欢迎来到西贡中央邮局，地址是巴黎公社街二号，靠近圣母大教堂。这里让人想起书信和电报曾是连接远方亲友的重要纽带。

                大楼建于一八八六年至一八九一年。走入内部，拱形屋顶与大厅带来不同于外立面的视角。根据越南邮政的介绍，这里保存着两幅历史地图：一幅描绘一八九二年的西贡及周边地区，另一幅描绘一九三六年越南南部与柬埔寨的电报网络。

                不妨抬头观察建筑结构，再花些时间看看地图。这些线条能帮助我们想象前人如何连接各地、记录生活空间。把这次参观当作昔日通信方式与今天即时消息的一次相遇吧。拍照时请选择合适的位置，保持安静，不要妨碍办理邮政业务的人。
                """,
            ["ja"] = """
                サイゴン中央郵便局へようこそ。所在地はパリ公社通り二番地で、サイゴン大教会の近くです。手紙や電報が、遠く離れた人々をつなぐ大切な手段だった時代を思い起こさせます。

                建物は一八八六年から一八九一年にかけて建設されました。内部のアーチ状の天井とホールでは、外観とは違う印象を楽しめます。ベトナム郵便の紹介によると、二枚の歴史的な地図が保存されています。一枚は一八九二年のサイゴンとその周辺、もう一枚は一九三六年のベトナム南部とカンボジアの電信網を示しています。

                上部の構造を見上げてから、地図を眺めてみましょう。昔の人々が各地を結び、生活空間を記録した方法に思いを巡らせることができます。昔の通信と現代のメッセージが出会う場所として楽しんでください。写真は適切な場所で撮り、静かに見学し、郵便サービスを利用する人の通行を妨げないようにしましょう。
                """,
            ["ko"] = """
                사이공 중앙우체국에 오신 것을 환영합니다. 파리 코뮌 거리 2번지, 노트르담 대성당 근처에 있습니다. 편지와 전보가 멀리 떨어진 사람들을 잇는 중요한 수단이었던 시대를 떠올리게 하는 장소입니다.

                건물은 1886년부터 1891년까지 지어졌습니다. 내부의 아치형 천장과 홀은 외관과 다른 시선을 제공합니다. 베트남 우정의 소개에 따르면 이곳에는 두 점의 역사적 지도가 보존되어 있습니다. 하나는 1892년 사이공과 주변 지역을, 다른 하나는 1936년 베트남 남부와 캄보디아의 전신망을 보여 줍니다.

                위쪽 구조를 올려다본 뒤 지도를 살펴보세요. 옛사람들이 장소를 연결하고 생활 공간을 기록한 방식을 상상할 수 있습니다. 과거의 통신과 오늘날의 즉석 메시지가 만나는 시간으로 즐겨 보세요. 사진은 적절한 위치에서 찍고, 조용히 관람하며 우편 업무를 보는 사람들의 이동을 막지 않도록 해 주세요.
                """,
            ["fr"] = """
                Bienvenue à la poste centrale de Saigon, au 2, rue Cong Xa Paris, près de la cathédrale Notre-Dame. Ce lieu rappelle une époque où lettres et télégrammes reliaient les personnes éloignées.

                Le bâtiment a été construit entre 1886 et 1891. À l'intérieur, la voûte et le hall offrent une perspective différente de la façade. Selon Vietnam Post, deux cartes historiques y sont conservées : l'une représente Saigon et ses environs en 1892, l'autre le réseau télégraphique du sud du Vietnam et du Cambodge en 1936.

                Levez les yeux vers la structure, puis observez les cartes. Leurs lignes invitent à imaginer comment les générations précédentes reliaient les lieux et représentaient leur environnement. Voyez cette visite comme une rencontre entre la communication d'autrefois et nos messages instantanés. Choisissez un endroit adapté pour vos photos, parlez doucement et ne gênez pas les personnes utilisant les services postaux.
                """ }),
        new DemoNarration("Phố đi bộ Nguyễn Huệ",
            "Phố đi bộ Nguyễn Huệ là không gian đi bộ và sinh hoạt văn hóa ở trung tâm Quận 1, hướng về sông Sài Gòn.",
            "https://vietnamtourism.vn/index.php/tourism/items/2905/2", new() {
            ["vi"] = """
                Hãy cùng khám phá Phố đi bộ Nguyễn Huệ, một không gian để đi dạo và cảm nhận nhịp sống ở trung tâm thành phố. Bạn có thể nhìn về phía trụ sở Ủy ban Nhân dân thành phố, rồi hướng mắt dọc tuyến phố về phía sông Sài Gòn.

                Trước khi trở thành đại lộ, khu vực này từng là kênh Chợ Vải. Năm 1887, con kênh được lấp để hình thành tuyến đường. Không gian phố đi bộ mở cửa đón người dân vào tháng tư năm 2015. Theo giới thiệu của cơ quan du lịch, phần quảng trường từ đường Lê Lợi đến đường Tôn Đức Thắng có lối đi bộ ở giữa, còn phương tiện vẫn lưu thông ở hai bên.

                Hãy thử tạm cất điện thoại và quan sát cách mọi người sử dụng không gian chung: đi dạo, gặp gỡ hoặc ghi lại một khoảnh khắc bên người thân. Nếu muốn tìm góc chụp ảnh, bạn có thể dừng ở nơi thoáng và nhìn lại chiều dài tuyến phố. Khi di chuyển sang hai bên đường, hãy chú ý xe cộ và sử dụng lối qua đường phù hợp. Giữ sạch cảnh quan cũng là cách để mỗi người góp phần làm chuyến dạo phố dễ chịu hơn.
                """,
            ["en"] = """
                Let's explore Nguyen Hue Walking Street, a place to stroll and experience life in the city centre. Look towards City Hall, then along the boulevard towards the Saigon River.

                Before it became a boulevard, this area was the Cho Vai canal. The canal was filled in 1887 to create a road. The pedestrian space opened to the public in April 2015. The tourism authority describes the square between Le Loi and Ton Duc Thang streets as a central pedestrian area with traffic continuing on both sides.

                Put your phone away for a moment and watch how people share this space: walking, meeting friends or making memories with family. For a photograph, stop somewhere spacious and look back along the boulevard. Watch for vehicles when moving to either side, and use appropriate crossings. Keeping the area clean helps make everyone's stroll more enjoyable.
                """,
            ["zh"] = """
                一起来探索阮惠步行街，漫步并感受市中心的生活节奏吧。您可以先望向市人民委员会办公楼，再沿着街道朝西贡河的方向远眺。

                在成为大道之前，这里曾是布市运河。一八八七年，运河被填平并改建为道路。步行空间于二〇一五年四月向公众开放。旅游部门介绍，黎利街至孙德胜街之间的广场中央是步行区，两侧仍有车辆通行。

                不妨暂时收起手机，看看人们如何共享空间：散步、会面，或与家人留下回忆。拍照时可以选择宽敞的位置，回望街道的延伸。走向两侧时请留意车辆，并使用合适的过街通道。保持环境整洁，也能让每个人的漫步更加舒适。
                """,
            ["ja"] = """
                グエンフエ歩行者通りで、中心街の暮らしを感じながら歩いてみましょう。市の人民委員会庁舎を眺めてから、通りに沿ってサイゴン川の方向へ目を向けてみてください。

                大通りになる前、この場所にはチョーヴァイ運河がありました。一八八七年に運河が埋め立てられ、道路になりました。歩行者空間が一般に開放されたのは二〇一五年四月です。観光当局によると、レロイ通りからトンドゥックタン通りまでの広場は中央が歩行者用で、その両側には車が通ります。

                少しだけ携帯電話をしまい、人々が散歩や待ち合わせ、家族との時間を楽しむ様子を眺めてみましょう。写真を撮るときは広い場所で立ち止まり、通りを振り返ってみてください。両側へ移動するときは車に注意し、適切な横断場所を利用しましょう。周囲をきれいに保つことも、気持ちよい散策につながります。
                """,
            ["ko"] = """
                응우옌후에 보행자 거리를 걸으며 도심의 생활을 느껴 보세요. 시 인민위원회 청사를 바라본 다음, 거리를 따라 사이공강 쪽으로 시선을 옮겨 보세요.

                대로가 되기 전 이곳에는 쩌바이 운하가 있었습니다. 1887년에 운하를 메워 도로를 만들었고, 보행 공간은 2015년 4월에 시민들에게 개방되었습니다. 관광 당국의 소개에 따르면 레러이 거리와 똔득탕 거리 사이 광장은 중앙이 보행 구역이며 양옆으로는 차량이 통행합니다.

                잠시 휴대전화를 내려놓고 사람들이 산책하거나 만나고 가족과 추억을 남기는 모습을 살펴보세요. 사진을 찍고 싶다면 넓은 곳에 멈춰 거리의 길이를 되돌아보세요. 양쪽 도로로 이동할 때는 차량에 주의하고 적절한 횡단 구역을 이용하세요. 주변을 깨끗이 유지하는 일도 모두에게 더 즐거운 산책을 만들어 줍니다.
                """,
            ["fr"] = """
                Découvrons la rue piétonne Nguyen Hue, un espace pour se promener et ressentir la vie du centre-ville. Regardez vers l'hôtel de ville, puis le long du boulevard en direction de la rivière Saigon.

                Avant de devenir une avenue, cet endroit était le canal Cho Vai. Il a été comblé en 1887 pour créer une route. L'espace piéton a ouvert au public en avril 2015. Selon l'organisme du tourisme, la place entre les rues Le Loi et Ton Duc Thang comprend une promenade centrale, avec une circulation automobile de chaque côté.

                Rangez un instant votre téléphone et observez les promenades, les rencontres et les moments partagés en famille. Pour prendre une photo, arrêtez-vous dans un endroit dégagé et regardez la perspective du boulevard. Faites attention aux véhicules en rejoignant les côtés et empruntez les passages adaptés. Garder cet espace propre contribue au plaisir de tous.
                """ }),
        new DemoNarration("Nhà hát Thành phố",
            "Nhà hát Thành phố nằm tại số 7 Công trường Lam Sơn, khu vực Quận 1, là địa điểm biểu diễn nghệ thuật.",
            "https://csdl.vietnamtourism.gov.vn/dest/?item=381", new() {
            ["vi"] = """
                Điểm dừng của chúng ta là Nhà hát Thành phố, tại số bảy Công trường Lam Sơn. Giữa khu phố trung tâm, công trình gợi mở một cách khám phá Sài Gòn qua kiến trúc và nghệ thuật biểu diễn.

                Nhà hát được xây dựng từ năm 1898 và khánh thành vào ngày một tháng một năm 1900. Mặt tiền có nhiều chi tiết trang trí, tượng và phù điêu. Khi quan sát, bạn có thể lần lượt nhìn từ các bậc thềm lên hệ cửa và phần mái để cảm nhận bố cục của công trình. Theo cơ sở dữ liệu ngành du lịch, nơi đây tổ chức các hoạt động sân khấu, âm nhạc và những sự kiện văn hóa.

                Nếu chỉ ghé ngang, hãy dành vài phút đứng ở vị trí an toàn để ngắm mặt tiền, thay vì bước xuống lòng đường tìm góc ảnh. Nếu muốn xem biểu diễn, bạn nên kiểm tra chương trình và thông tin vé do đơn vị tổ chức công bố cho ngày dự định đến. Không phải lúc nào nhà hát cũng mở để tham quan bên trong. Dù dừng lại ngắn hay theo dõi một chương trình, bạn vẫn có thể xem đây là lời mời tìm hiểu đời sống nghệ thuật của thành phố.
                """,
            ["en"] = """
                Our stop is the Municipal Theatre, also known as Saigon Opera House, at 7 Cong Truong Lam Son. In the central district, this building invites you to explore Saigon through architecture and the performing arts.

                Construction began in 1898, and the theatre opened on January 1, 1900. Its facade includes decorative details, statues and reliefs. Let your eyes move from the steps to the doors and roof to appreciate its composition. The tourism database describes it as a venue for theatre, music and cultural events.

                If passing by, take a few minutes to view the facade from a safe place rather than stepping into traffic for a photograph. To attend a performance, check the organiser's programme and ticket information for your chosen date. Interior sightseeing is not always available. Whether stopping briefly or attending a show, this can be an invitation to discover the city's artistic life.
                """,
            ["zh"] = """
                这一站是位于蓝山广场七号的市立剧院，也称西贡歌剧院。在市中心，这座建筑邀请我们从建筑和表演艺术的角度认识西贡。

                剧院于一八九八年开工，一九〇〇年一月一日启用。外立面有装饰细节、雕像和浮雕。您可以从台阶逐渐看向门窗与屋顶，感受建筑的构图。旅游数据库将这里介绍为举办戏剧、音乐及文化活动的场所。

                如果只是路过，可以站在安全的位置花几分钟欣赏外观，不要为了拍照走到车道上。若想观看演出，请查阅主办方针对计划到访日期公布的节目和票务信息。剧院内部并非随时开放参观。无论短暂停留还是欣赏演出，都可以把这里当作了解城市艺术生活的一个起点。
                """,
            ["ja"] = """
                今回の目的地は、ラムソン広場七番地の市民劇場です。サイゴン・オペラハウスとも呼ばれ、建築と舞台芸術から街を知るきっかけになります。

                一八九八年に建設が始まり、一九〇〇年一月一日に開館しました。正面には彫像や浮き彫りなどの装飾があります。階段から入口、屋根へと順に目を向け、建物全体の構成を眺めてみましょう。観光データベースでは、演劇や音楽、文化イベントを行う場所として紹介されています。

                通りがかりなら、安全な場所から数分間、外観を楽しんでください。撮影のために車道へ出ないようにしましょう。公演を見たい場合は、訪問予定日のプログラムとチケット情報を主催者の案内で確認してください。内部見学が常にできるわけではありません。短い立ち寄りでも公演鑑賞でも、街の芸術に触れる入口になるでしょう。
                """,
            ["ko"] = """
                이번 목적지는 람선 광장 7번지의 시립극장입니다. 사이공 오페라하우스라고도 불리며, 건축과 공연 예술을 통해 도시를 살펴보게 하는 장소입니다.

                극장은 1898년에 공사를 시작해 1900년 1월 1일에 문을 열었습니다. 정면에는 장식과 조각상, 부조가 있습니다. 계단에서 출입문과 지붕으로 시선을 옮기며 건물의 구성을 살펴보세요. 관광 데이터베이스는 이곳을 연극, 음악과 문화 행사가 열리는 장소로 소개합니다.

                지나가는 길이라면 차도로 내려가 사진을 찍기보다 안전한 곳에서 잠시 외관을 감상하세요. 공연을 관람하려면 방문하려는 날짜에 맞춰 주최 측이 공개한 프로그램과 예매 정보를 확인하는 것이 좋습니다. 내부 관광이 항상 가능한 것은 아닙니다. 잠깐 들르든 공연을 보든, 도시의 예술 생활을 알아가는 출발점이 될 수 있습니다.
                """,
            ["fr"] = """
                Nous voici au théâtre municipal, également appelé Opéra de Saigon, au 7, place Cong Truong Lam Son. Dans le centre-ville, ce bâtiment invite à découvrir Saigon par son architecture et ses arts de la scène.

                Sa construction a commencé en 1898 et il a été inauguré le premier janvier 1900. La façade présente des ornements, des statues et des reliefs. Observez successivement les marches, les portes et le toit pour apprécier sa composition. La base de données touristique le décrit comme un lieu de théâtre, de musique et d'événements culturels.

                Si vous passez devant, admirez la façade depuis un endroit sûr plutôt que de descendre sur la chaussée pour une photo. Pour assister à un spectacle, consultez le programme et les informations de billetterie de l'organisateur pour la date choisie. La visite intérieure n'est pas toujours possible. Une halte ou une représentation peuvent donner envie de découvrir la vie artistique de la ville.
                """ })
    });

    public static DemoNarration For(string name) => All.Single(item => item.Name == name);

    public TranslationResult? Find(string source, string language)
    {
        var script = All.FirstOrDefault(item => string.Equals(item.Texts["vi"], source, StringComparison.Ordinal));
        return language != "vi" && script?.Texts.TryGetValue(language, out var text) == true ? new(text, language, Provider, true) : null;
    }
}
