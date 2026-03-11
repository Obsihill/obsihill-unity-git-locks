# Unity Git Locks

An editor tool to manipulate git LFS locks efficiently inside the Unity Editor.  
유니티 에디터 내에서 Git LFS 잠금(Locks)을 효율적으로 관리할 수 있는 에디터 툴입니다.

---

## 🇬🇧 English

### Installation (via Unity Package Manager)
You can install this package directly from GitHub using the Unity Package Manager (UPM).

1. Open Unity and go to **Window > Package Manager**.
2. Click the **+** button in the top left corner.
3. Select **"Add package from git URL..."**
4. Paste the following URL and click **Add**:
   ```text
   [https://github.com/Obsihill/obsihill-unity-git-locks](https://github.com/Obsihill/obsihill-unity-git-locks.git)
   ```

### Usage
Once installed, the Unity Git Locks tool will automatically integrate into your editor.

1. **Open the Tool**: Go to **Window > Git Locks** in the top menu.
2. **View Locks**: The window will display a tree view of your project's folders and files, highlighting which files are currently locked.
3. **Lock/Unlock**: Select any file or folder in the tree view and click the **Lock** or **Unlock** button at the bottom of the window to manage its Git LFS state.
4. **Context Menu**: You can also easily lock or unlock items by right-clicking them directly in the Unity **Project** (Assets) or **Hierarchy** (GameObject) window.

---

## 🇰🇷 한국어

### 설치 방법 (Unity Package Manager 사용)
Unity Package Manager (UPM)를 통해 GitHub URL로 패키지를 쉽게 설치할 수 있습니다.

1. 유니티에서 **Window > Package Manager**를 엽니다.
2. 창 왼쪽 상단의 **+** 버튼을 클릭합니다.
3. **"Add package from git URL..."** 을 선택합니다.
4. 아래 URL을 복사하여 붙여넣고 **Add** 버튼을 누릅니다:
   ```text
   [https://github.com/Obsihill/obsihill-unity-git-locks](https://github.com/Obsihill/obsihill-unity-git-locks.git)
   ```

### 사용 가이드
설치가 완료되면 유니티 에디터에 자동으로 툴이 통합됩니다.

1. **툴 실행**: 상단 메뉴바에서 **Window > Git Locks**를 클릭하여 창을 엽니다.
2. **잠금 확인**: 창 안에서 프로젝트 구조가 트리 뷰 형태로 표시되며, 현재 LFS 잠금된 파일들을 한눈에 확인할 수 있습니다.
3. **잠금/해제**: 트리 뷰에서 파일이나 폴더를 선택한 후, 창 하단의 **Lock** 또는 **Unlock** 버튼을 눌러 상태를 변경합니다.
4. **우클릭 메뉴 연동**: 유니티의 **Project(Assets) 창**이나 **Hierarchy(GameObject) 창**에서도 에셋을 우클릭하여 간편하게 잠금(Lock) 및 해제(Unlock)를 수행할 수 있습니다.
