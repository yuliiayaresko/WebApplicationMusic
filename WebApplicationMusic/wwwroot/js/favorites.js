let favoriteAlbums = [];

function getFavoriteAlbums() {
    const userId = localStorage.getItem('userId') || 1;
    fetch(`/api/FavoriteAlbums/user/${userId}`, {
        headers: {
            'Accept-Encoding': 'identity'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            _displayFavoriteAlbums(data);
        })
        .catch(error => {
            console.error('Помилка при отриманні улюблених альбомів:', error);
            alert('Не вдалося завантажити улюблені альбоми. Перевірте консоль для деталей.');
            _displayFavoriteAlbums([]); // Показати порожній список у разі помилки
        });
}

function displayAlbumDetails(id) {
    const cachedAlbum = favoriteAlbums.find(album => album.id === id);
    if (cachedAlbum) {
        _renderAlbumDetails(cachedAlbum);
    } else {
        alert('Альбом не знайдено в улюблених.');
    }
}

function _renderAlbumDetails(album) {
    document.getElementById('album-details-cover').src = album.coverImageUrl || '/images/placeholder.png';
    document.getElementById('album-details-title').textContent = album.title;
    document.getElementById('album-details-year').textContent = ` ${album.releaseYear}`;
    document.getElementById('album-details-artist').textContent = ` ${album.artistName}`;

    const songsList = document.getElementById('album-details-songs');
    songsList.innerHTML = '';
    if (album.songs && album.songs.length > 0) {
        album.songs.forEach(song => {
            const li = document.createElement('li');
            li.textContent = `${song.title} (${song.duration})`;
            songsList.appendChild(li);
        });
    } else {
        songsList.innerHTML = '<li>Пісень немає.</li>';
    }

    document.getElementById('albumDetailsOverlay').style.display = 'block';
    document.getElementById('albumDetailsModal').style.display = 'block';
}

function _displayFavoriteAlbums(data) {
    const container = document.getElementById('favorites');
    container.innerHTML = '';

    console.log('Отримані дані:', data);

    if (!Array.isArray(data) || data.length === 0) {
        container.innerHTML = '<p>У вас поки немає улюблених альбомів.</p>';
        return;
    }

    data.forEach(album => {
        const card = document.createElement('div');
        card.className = 'album-card';

        const image = document.createElement('img');
        image.className = 'album-image';
        image.src = album.coverImageUrl || '/images/placeholder.png';
        image.alt = `${album.title} cover`;

        const title = document.createElement('h4');
        title.textContent = album.title;

        const year = document.createElement('p');
        year.textContent = ` ${album.releaseYear}`;

        const artistName = document.createElement('p');
        artistName.textContent = ` ${album.artistName}`;

        const albumInfo = document.createElement('div');
        albumInfo.className = 'album-info';
        albumInfo.setAttribute('onclick', `displayAlbumDetails(${album.id})`);
        albumInfo.appendChild(image);
        albumInfo.appendChild(title);
        albumInfo.appendChild(year);
        albumInfo.appendChild(artistName);

        card.appendChild(albumInfo);
        container.appendChild(card);
    });

    favoriteAlbums = data;
}

document.addEventListener('DOMContentLoaded', () => {
    getFavoriteAlbums();
});