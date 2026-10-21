namespace ShortVideoTranslator.Data;

// Original demo scripts. Source facts and image dates are documented in docs/DISTRICT1-EXPANSION.md.
// These language drafts use the same explicit SmartTourDraft provenance as the existing samples.
internal static class AdditionalDemoNarrations
{
    public static readonly DemoNarration[] All = [
        new("Nhà thờ Đức Bà Sài Gòn", "", "https://vietnamtourism.vn/index.php/tourism/items/270/3", new() {
            ["vi"] = """
                Chào mừng bạn đến với Nhà thờ Đức Bà Sài Gòn, tại số một Công xã Paris, trong khu vực trung tâm Quận 1. Hãy tìm một vị trí an toàn trên vỉa hè để quan sát toàn cảnh công trình, thay vì dừng lại giữa dòng xe. Đây là một không gian tôn giáo; cách tham quan phù hợp bắt đầu từ sự yên tĩnh và tôn trọng người đang hành lễ.

                Theo giới thiệu của Cục Du lịch Quốc gia Việt Nam, nhà thờ được khởi công năm 1877 và khánh thành năm 1880. Khi nhìn từ bên ngoài, bạn có thể chú ý những mảng tường gạch, nhịp cửa vòm và hai tháp chuông. Thử quan sát từ một vài góc đứng khác nhau để cảm nhận cách các chi tiết tạo nên hình dáng công trình, rồi đối chiếu với ảnh tư liệu trong bộ sưu tập của ứng dụng.

                Ảnh trên ứng dụng được ghi năm chụp và không khẳng định diện mạo hiện tại. Trước khi đến, hãy kiểm tra thông báo của nhà thờ về khu vực được tiếp cận, công việc trùng tu và thời gian hành lễ. Nếu không thể vào bên trong, bạn vẫn có thể tìm hiểu kiến trúc từ bên ngoài. Hãy giữ lối đi thông thoáng và tránh chụp ảnh làm ảnh hưởng đến sinh hoạt tôn giáo.
                """,
            ["en"] = """
                Welcome to Saigon Notre-Dame Cathedral at 1 Cong Xa Paris, in the District 1 area. Find a safe place on the pavement to view the building instead of stopping among traffic. This is a religious space: a considerate visit begins with quiet behaviour and respect for people attending worship.

                Vietnam's national tourism authority dates construction to 1877–1880. From outside, notice the brick walls, arched openings and twin bell towers. Try viewing the building from different safe positions and compare the details with the dated photographs in this guide.

                The photographs are historical views, not a statement about the cathedral's present appearance. Before visiting, check the cathedral's notices about access, restoration work and services. If interior access is unavailable, you can still appreciate the architecture from outside. Keep walkways clear and avoid taking photographs that disturb worshippers.
                """,
            ["zh"] = """
                欢迎来到西贡圣母大教堂，地址是第一郡区域的公社巴黎广场一号。请在人行道上寻找安全的位置观察建筑，不要停留在车流中。这是一处宗教场所，参观时请保持安静，尊重正在参加礼拜的人。

                根据越南国家旅游部门的介绍，教堂于一八七七年开工，一八八〇年落成。从外部可以留意砖墙、拱形门窗和双钟楼。试着从几个安全的位置欣赏建筑，再与应用中标注拍摄年份的照片进行比较。

                这些照片是过去的影像，并不代表教堂现在的外观。到访前，请查看教堂关于开放区域、修缮和礼拜时间的通知。如果无法进入室内，仍可从外部欣赏建筑。请保持通道畅通，拍照时避免打扰宗教活动。
                """,
            ["ja"] = """
                サイゴン大教会へようこそ。場所は一郡エリアのコン・サー・パリ広場一番地です。車道に立ち止まらず、歩道の安全な場所から建物を眺めましょう。宗教施設なので、静かに過ごし、礼拝中の人への配慮を大切にしてください。

                ベトナムの観光当局によると、教会は一八七七年に着工し、一八八〇年に完成しました。外からは、れんがの壁、アーチ形の開口部、二つの鐘楼に注目できます。安全な場所から角度を変えて眺め、このガイドの撮影年付き写真と比べてみましょう。

                掲載写真は過去の記録であり、現在の外観を示すものではありません。訪問前に、立ち入り可能な区域、修復工事、礼拝について教会の案内を確認してください。内部に入れなくても、外観の建築を楽しめます。通路をふさがず、宗教活動を妨げる撮影は控えましょう。
                """,
            ["ko"] = """
                사이공 노트르담 대성당에 오신 것을 환영합니다. 위치는 1군 지역의 꽁싸파리 광장 1번지입니다. 차도에 멈추지 말고 보도의 안전한 곳에서 건물을 감상하세요. 종교 시설이므로 조용히 행동하고 예배 중인 사람들을 배려해 주세요.

                베트남 관광 당국의 소개에 따르면 성당은 1877년에 착공하여 1880년에 완공되었습니다. 밖에서 벽돌 벽, 아치형 창과 문, 두 개의 종탑을 살펴보세요. 안전한 곳에서 시선을 달리하며 이 가이드에 촬영 연도가 표시된 사진과 비교해 볼 수 있습니다.

                사진은 과거의 기록이며 현재 모습을 보장하지 않습니다. 방문 전에는 접근 가능한 구역, 복원 공사와 예배에 관한 성당의 안내를 확인하세요. 내부에 들어갈 수 없더라도 외부 건축을 감상할 수 있습니다. 통로를 비워 두고 종교 활동을 방해하는 촬영은 삼가 주세요.
                """,
            ["fr"] = """
                Bienvenue à la cathédrale Notre-Dame de Saïgon, au 1, place Cong Xa Paris, dans le secteur du premier arrondissement. Observez le bâtiment depuis un endroit sûr sur le trottoir, sans vous arrêter dans la circulation. Ce lieu religieux appelle au calme et au respect des personnes qui viennent prier.

                L'autorité nationale du tourisme situe sa construction entre 1877 et 1880. De l'extérieur, remarquez les murs de brique, les ouvertures en arc et les deux clochers. Changez de point de vue en restant en sécurité et comparez les détails avec les photographies datées de ce guide.

                Ces images sont des vues anciennes et ne garantissent pas l'apparence actuelle. Avant de venir, consultez les annonces de la cathédrale concernant l'accès, les travaux et les offices. Si l'intérieur est inaccessible, l'architecture reste visible depuis l'extérieur. Laissez les passages libres et évitez les photographies qui dérangent les fidèles.
                """ }),
        new("Bảo tàng Mỹ thuật TP.HCM", "", "https://www.baotangmythuattphcm.com.vn/tham-quan", new() {
            ["vi"] = """
                Điểm dừng hôm nay là Bảo tàng Mỹ thuật Thành phố Hồ Chí Minh, tại số chín mươi bảy A, đường Phó Đức Chính, trong khu vực Quận 1. Trước khi bước vào phòng trưng bày, hãy dành một chút thời gian nhìn mặt tiền và không gian quanh tòa nhà. Việc quan sát kiến trúc cũng có thể là phần mở đầu cho một cuộc gặp gỡ với nghệ thuật.

                Theo trang giới thiệu của bảo tàng, nội dung sưu tập gồm mỹ thuật cổ, cận đại và mỹ thuật hiện đại. Khi đứng trước một tác phẩm, bạn có thể đọc tên, chất liệu và phần giới thiệu trên nhãn trưng bày, rồi tự chọn một chi tiết để quan sát kỹ hơn. Đó có thể là một mảng màu, một đường nét hoặc cách người nghệ sĩ tổ chức không gian. Không cần phải biết trước mọi thuật ngữ để bắt đầu thưởng thức nghệ thuật.

                Hãy di chuyển chậm, giữ khoảng cách với tác phẩm và làm theo hướng dẫn của nhân viên. Trước khi quay phim hoặc chụp ảnh, cần kiểm tra quy định tại khu vực đang tham quan. Giá vé, lịch mở cửa và triển lãm có thể thay đổi; ứng dụng không thay thế thông báo chính thức của bảo tàng. Kết thúc chuyến đi, thử nhớ lại một tác phẩm khiến bạn muốn dừng lâu hơn và tự kể câu chuyện cảm nhận của mình.
                """,
            ["en"] = """
                Our next stop is the Ho Chi Minh City Museum of Fine Arts at 97A Pho Duc Chinh Street, in the District 1 area. Before entering a gallery, take a moment to look at the building and its surroundings. Observing architecture can be the beginning of an encounter with art.

                The museum describes collections covering ancient, early modern and modern fine arts. Read a work's title, material and display label, then choose one detail to examine closely: a colour, a line or the way space is arranged. You do not need to know every technical term to begin enjoying art.

                Move slowly, keep a respectful distance from the works and follow staff instructions. Check the rules before filming or taking photographs. Tickets, opening arrangements and exhibitions may change; this guide does not replace the museum's official notices. At the end, recall a work that made you pause and describe your own response to it.
                """,
            ["zh"] = """
                接下来是胡志明市美术馆，地址为第一郡区域副德政街九十七A号。进入展厅前，不妨先看看建筑外观及周围空间。观察建筑本身，也可以成为一次艺术体验的开始。

                美术馆介绍的馆藏包括古代、近代与现代美术。站在一件作品前，可以先阅读名称、材料和展签，再挑选一个细节仔细观察，例如色彩、线条或空间的安排。欣赏艺术不需要预先掌握全部专业术语。

                请放慢脚步，与作品保持距离，并遵守工作人员的指引。摄影和录像前，请确认所在区域的规定。门票、开放安排及展览可能调整，本应用不能代替美术馆的正式通知。离开时，试着回想一件让你愿意多停留片刻的作品，并说出自己的感受。
                """,
            ["ja"] = """
                次はホーチミン市美術館です。一郡エリアのフォー・ドゥック・チン通り九十七A番地にあります。展示室に入る前に、建物の外観や周囲の空間にも目を向けてみましょう。建築を観察することも、芸術との出会いの始まりになります。

                美術館の案内では、古代、近代、現代の美術を扱っています。作品の前では、題名、素材、展示ラベルを読み、色や線、空間の構成など、一つの細部を選んで眺めてみてください。専門用語をすべて知らなくても、鑑賞を始めることができます。

                ゆっくり移動し、作品との距離を保ち、職員の案内に従いましょう。撮影前には各展示室の規則を確認してください。料金、開館状況、展示内容は変わることがあるため、このガイドは美術館の正式な案内に代わるものではありません。帰るときには、心に残った作品とその理由を思い返してみましょう。
                """,
            ["ko"] = """
                다음은 호찌민시 미술관입니다. 1군 지역의 포득찐 거리 97A번지에 있습니다. 전시실에 들어가기 전에 건물의 외관과 주변 공간을 잠시 바라보세요. 건축을 관찰하는 것도 예술을 만나는 시작이 될 수 있습니다.

                미술관은 고대, 근대와 현대 미술을 아우르는 소장품을 소개합니다. 작품 앞에서는 제목, 재료와 설명을 읽고 색채, 선 또는 공간 구성 중 한 가지를 골라 자세히 살펴보세요. 모든 전문 용어를 미리 알아야만 예술을 즐길 수 있는 것은 아닙니다.

                천천히 이동하고 작품과 거리를 유지하며 직원의 안내를 따라 주세요. 촬영하기 전에는 해당 전시 공간의 규정을 확인하세요. 입장료, 운영 일정과 전시는 변경될 수 있으므로 이 가이드가 미술관의 공식 공지를 대신하지는 않습니다. 관람을 마칠 때 오래 바라보고 싶었던 작품 하나와 그 이유를 떠올려 보세요.
                """,
            ["fr"] = """
                Notre prochaine étape est le musée des Beaux-Arts de Hô Chi Minh-Ville, au 97A, rue Pho Duc Chinh, dans le secteur du premier arrondissement. Avant d'entrer dans une salle, observez le bâtiment et ses abords. L'architecture peut déjà ouvrir la rencontre avec l'art.

                Le musée présente des collections d'art ancien, de périodes plus récentes et d'art moderne. Devant une œuvre, lisez son titre, son matériau et son cartel, puis choisissez un détail à observer : une couleur, une ligne ou l'organisation de l'espace. Il n'est pas nécessaire de connaître tout le vocabulaire spécialisé pour apprécier une œuvre.

                Avancez lentement, gardez vos distances avec les œuvres et suivez les indications du personnel. Vérifiez les règles avant de filmer ou de photographier. Tarifs, horaires et expositions peuvent évoluer ; ce guide ne remplace pas les annonces du musée. À la fin, repensez à une œuvre qui vous a donné envie de vous arrêter et exprimez votre propre impression.
                """ }),
        new("Công viên Tao Đàn", "", "https://visithcmc.net/news/top-9-cong-vien-lon-nhat-o-tphcm", new() {
            ["vi"] = """
                Hãy dành một khoảng nghỉ trong hành trình khám phá Quận 1 tại Công viên Tao Đàn. Địa chỉ tham khảo là số năm mươi lăm C, đường Nguyễn Thị Minh Khai. Trước khi bắt đầu đi bộ, bạn có thể quan sát bảng chỉ dẫn ở lối vào để chọn hướng đi phù hợp và ghi nhớ điểm hẹn nếu đi cùng bạn bè.

                Trung tâm Xúc tiến Du lịch Thành phố Hồ Chí Minh giới thiệu Tao Đàn là một không gian nhiều cây xanh, có lối dạo bộ và các hoạt động thể dục, sinh hoạt cộng đồng. Thay vì vội đi qua, hãy thử chú ý những thay đổi của ánh sáng dưới tán cây, khoảng cách giữa các thân cây và nhịp chuyển động của người đi bộ. Một công viên có thể giúp chúng ta cảm nhận thành phố qua những sinh hoạt thường ngày.

                Bạn có thể chọn một đoạn đường vừa sức để dạo bộ và dừng nghỉ khi cần. Hãy giữ vệ sinh, không giẫm lên khu vực trồng cây và nhường lối cho người khác. Nếu có sự kiện hoặc khu vực tạm hạn chế, cần theo hướng dẫn tại chỗ; ảnh trong ứng dụng không bảo đảm mọi lối vào đang mở. Trước khi rời công viên, kiểm tra lại đồ dùng cá nhân và chọn lối ra an toàn để tiếp tục chuyến khám phá.
                """,
            ["en"] = """
                Take a break from exploring District 1 at Tao Dan Park. The reference address is 55C Nguyen Thi Minh Khai Street. Before walking, check the signs at the entrance and agree on a meeting point if you are visiting with friends.

                Ho Chi Minh City's tourism promotion centre describes Tao Dan as a green space with walking paths, exercise and community activities. Notice how light changes beneath the trees, the spacing between their trunks and the movement of people along the paths. A park offers a way to experience the city's everyday rhythms.

                Choose a comfortable walking distance and rest when needed. Keep the grounds clean, avoid planted areas and make room for others. Follow local instructions during events or temporary restrictions: photographs in this guide do not guarantee that every entrance is open. Before leaving, check your belongings and choose a safe exit for the next part of your visit.
                """,
            ["zh"] = """
                探索第一郡时，可以在陶坛公园稍作休息。参考地址是阮氏明开街五十五C号。开始散步前，请先看看入口处的指示牌；如果与朋友同行，也可以约好集合地点。

                胡志明市旅游促进中心将陶坛介绍为一处绿地，设有步行空间，也有人们锻炼和进行社区活动。不妨观察树荫下光线的变化、树干之间的间距，以及行人的步伐。公园让我们从日常生活的节奏感受城市。

                请选择适合自己的步行距离，需要时停下休息。保持环境整洁，不要踩踏种植区，并为其他人留出通道。如遇活动或临时限制，请遵守现场指引；应用中的照片不能保证所有入口都开放。离开前检查随身物品，选择安全的出口，继续接下来的行程。
                """,
            ["ja"] = """
                一郡を巡る途中、タオダン公園で一休みしてみましょう。参考住所はグエン・ティ・ミン・カイ通り五十五C番地です。歩き始める前に入口の案内を確認し、友人と一緒なら待ち合わせ場所を決めておくと安心です。

                ホーチミン市の観光促進機関は、タオダンを散歩や運動、地域の活動が行われる緑豊かな空間として紹介しています。木陰の光の変化、幹の間隔、道を歩く人の動きに目を向けてみてください。公園では街の日常のリズムを感じられます。

                無理のない距離を選び、必要なら休憩しましょう。ごみを残さず、植え込みに入らず、ほかの人に道を譲ってください。催しや一時的な立ち入り制限がある場合は、現地の案内に従いましょう。掲載写真はすべての入口が開いていることを保証しません。帰る前に持ち物を確かめ、安全な出口から次の目的地へ向かいましょう。
                """,
            ["ko"] = """
                1군을 둘러보는 동안 타오단 공원에서 잠시 쉬어 보세요. 참고 주소는 응우옌티민카이 거리 55C번지입니다. 걷기 전에 입구의 안내판을 살펴보고, 친구들과 함께라면 만날 장소를 정해 두세요.

                호찌민시 관광 진흥 기관은 타오단을 산책과 운동, 지역 활동이 이루어지는 녹지 공간으로 소개합니다. 나무 그늘 아래에서 달라지는 빛, 나무 사이의 간격, 길을 걷는 사람들의 움직임에 주목해 보세요. 공원에서는 도시의 일상적인 리듬을 느낄 수 있습니다.

                자신에게 편한 거리를 정하고 필요할 때 쉬어 가세요. 쓰레기를 남기지 말고 식재 구역을 밟지 않으며 다른 사람에게 길을 양보해 주세요. 행사나 일시적인 출입 제한이 있으면 현장 안내를 따르세요. 가이드의 사진이 모든 출입구의 개방을 보장하지는 않습니다. 떠나기 전에는 소지품을 확인하고 안전한 출구로 이동해 다음 여정을 이어 가세요.
                """,
            ["fr"] = """
                Faites une pause au parc Tao Dan pendant votre découverte du premier arrondissement. L'adresse de référence est le 55C, rue Nguyen Thi Minh Khai. Avant de marcher, consultez les panneaux à l'entrée et convenez d'un point de rendez-vous si vous êtes avec des amis.

                Le centre de promotion touristique de la ville décrit Tao Dan comme un espace vert propice à la promenade, à l'exercice et aux activités collectives. Observez la lumière sous les arbres, l'espace entre les troncs et le mouvement des promeneurs. Un parc permet de ressentir les rythmes quotidiens de la ville.

                Choisissez une distance adaptée et reposez-vous si nécessaire. Gardez les lieux propres, évitez les plantations et laissez passer les autres. Respectez les consignes lors d'événements ou de restrictions temporaires : les photos de ce guide ne garantissent pas l'ouverture de toutes les entrées. Avant de repartir, vérifiez vos affaires et prenez une sortie sûre pour poursuivre votre visite.
                """ })
    ];
}
