const uri = 'api/Albums';
let albums = [];

function getAlbums() {
    fetch(uri)
        .then(response => {
            if (!response.ok) {
                throw new Error(`Помилка HTTP! Статус: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            albums = data;
            return checkFavoritesForUser(1);
        })
        .then(favorites => _displayAlbums(albums, favorites))
        .catch(error => {
            console.error('Не вдалося отримати альбоми:', error);
            alert('Не вдалося завантажити альбоми. Перевірте з’єднання з сервером.');
        });
}

function checkFavoritesForUser(userId) {
    return fetch(`/api/Albums/favorites?userId=${userId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`Помилка HTTP! Статус: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            return data.map(favorite => ({
                id: favorite.id,
                albumId: favorite.albumId
            }));
        })
        .catch(error => {
            console.error('Не вдалося отримати улюблені альбоми:', error);
            return [];
        });
}

function searchAlbums() {
    const query = document.getElementById('search-query').value.trim();
    const url = query ? `${uri}/search?query=${encodeURIComponent(query)}` : uri;

    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error(`Помилка HTTP! Статус: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            albums = data;
            return checkFavoritesForUser(1);
        })
        .then(favorites => _displayAlbums(albums, favorites))
        .catch(error => {
            console.error('Не вдалося знайти альбоми:', error);
            alert('Не вдалося знайти альбоми. Перевірте з’єднання з сервером.');
        });
}

function resetSearch() {
    document.getElementById('search-query').value = '';
    getAlbums();
}

function searchFromNav() {
    const query = document.getElementById('nav-search-query').value.trim();
    document.getElementById('search-query').value = query;
    searchAlbums();
}

function addAlbum() {
    const addTitleTextbox = document.getElementById('add-title');
    const addYearTextbox = document.getElementById('add-year');
    const addArtistIdTextbox = document.getElementById('add-artistid');
    const addArtistNameTextbox = document.getElementById('add-artistname');
    const addCoverImageInput = document.getElementById('add-coverImage');

    const formData = new FormData();
    formData.append('Title', addTitleTextbox.value.trim());
    formData.append('ReleaseYear', parseInt(addYearTextbox.value, 10));
    formData.append('ArtistId', parseInt(addArtistIdTextbox.value, 10));
    formData.append('ArtistName', addArtistNameTextbox.value.trim());
    if (addCoverImageInput.files[0]) {
        formData.append('coverImage', addCoverImageInput.files[0]);
    }

    fetch(uri, {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`Помилка HTTP! Статус: ${response.status}`);
            }
            return response.json();
        })
        .then(() => {
            getAlbums();
            addTitleTextbox.value = '';
            addYearTextbox.value = '';
            addArtistIdTextbox.value = '';
            addArtistNameTextbox.value = '';
            addCoverImageInput.value = '';
            closeAddAlbumModal();
        })
        .catch(error => {
            console.error('Не вдалося додати альбом:', error);
            alert('Не вдалося додати альбом. Перевірте з’єднання з сервером.');
        });
}

function deleteAlbum(id) {
    fetch(`${uri}/${id}`, {
        method: 'DELETE'
    })
        .then(() => getAlbums())
        .catch(error => {
            console.error('Не вдалося видалити альбом:', error);
            alert('Не вдалося видалити альбом. Перевірте з’єднання з сервером.');
        });
}

function displayEditForm(id) {
    const album = albums.find(album => album.id === id);

    document.getElementById('edit-id').value = album.id;
    document.getElementById('edit-title').value = album.title;
    document.getElementById('edit-year').value = album.releaseYear;
    document.getElementById('edit-artistid').value = album.artistId;
    document.getElementById('edit-artistname').value = album.artistName || '';

    const previewContainer = document.getElementById('edit-image-preview');
    previewContainer.innerHTML = '';
    if (album.coverImageUrl) {
        const preview = document.createElement('img');
        preview.className = 'image-preview';
        preview.src = album.coverImageUrl;
        previewContainer.appendChild(preview);
    }

    document.getElementById('editAlbumOverlay').style.display = 'block';
    document.getElementById('editAlbum').style.display = 'block';
}

function updateAlbum() {
    const albumId = document.getElementById('edit-id').value;
    const editTitleTextbox = document.getElementById('edit-title');
    const editYearTextbox = document.getElementById('edit-year');
    const editArtistIdTextbox = document.getElementById('edit-artistid');
    const editArtistNameTextbox = document.getElementById('edit-artistname');
    const editCoverImageInput = document.getElementById('edit-coverImage');

    const formData = new FormData();
    formData.append('Id', albumId);
    formData.append('Title', editTitleTextbox.value.trim());
    formData.append('ReleaseYear', parseInt(editYearTextbox.value, 10));
    formData.append('ArtistId', parseInt(editArtistIdTextbox.value, 10));
    formData.append('ArtistName', editArtistNameTextbox.value.trim());

    const album = albums.find(a => a.id === parseInt(albumId));
    if (editCoverImageInput.files[0]) {
        formData.append('coverImage', editCoverImageInput.files[0]);
    } else if (album.coverImageUrl) {
        formData.append('CoverImageUrl', album.coverImageUrl);
    }

    fetch(`${uri}/${albumId}`, {
        method: 'PUT',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`Помилка HTTP! Статус: ${response.status}`);
            }
            getAlbums();
            closeInput();
        })
        .catch(error => {
            console.error('Не вдалося оновити альбом:', error);
            alert('Не вдалося оновити альбом. Перевірте з’єднання з сервером.');
        });
}

function closeInput() {
    document.getElementById('editAlbumOverlay').style.display = 'none';
    document.getElementById('editAlbum').style.display = 'none';
    document.getElementById('edit-image-preview').innerHTML = '';
}

async function displayAlbumDetails(id) {
    try {
        const response = await fetch(`/api/Albums/${id}`);
        if (!response.ok) {
            throw new Error(`Помилка HTTP! Статус: ${response.status}`);
        }
        const album = await response.json();

        document.getElementById('album-details-cover').src = album.coverImageUrl || '/images/placeholder.png';
        document.getElementById('album-details-title').textContent = album.title;
        document.getElementById('album-details-year').textContent = `Рік: ${album.releaseYear}`;
        document.getElementById('album-details-artist').textContent = `Артист: ${album.artistName || 'Невідомий артист'}`;

        const songsList = document.getElementById('album-details-songs');
        songsList.innerHTML = '';
        if (album.songs && album.songs.length > 0) {
            album.songs.forEach(song => {
                const li = document.createElement('li');
                li.textContent = `${song.title} (${song.artist || 'Невідомий виконавець'})`;
                songsList.appendChild(li);
            });
        } else {
            songsList.innerHTML = '<li>Пісень немає.</li>';
        }

        document.getElementById('albumDetailsOverlay').style.display = 'block';
        document.getElementById('albumDetailsModal').style.display = 'block';
    } catch (error) {
        console.error('Не вдалося завантажити деталі альбому:', error);
        alert('Не вдалося завантажити деталі альбому.');
    }
}

function _displayAlbums(data, favorites = []) {
    const container = document.getElementById('albums');
    container.innerHTML = '';

    data.forEach(album => {
        const card = document.createElement('div');
        card.className = 'album-card';

        const image = document.createElement('img');
        image.className = 'album-image';
        image.src = album.coverImageUrl || '/images/placeholder.png';
        image.alt = `${album.title} обкладинка`;
        image.onerror = () => { image.src = '/images/placeholder.png'; };

        const title = document.createElement('h4');
        title.textContent = album.title;

        const year = document.createElement('p');
        year.textContent = `Рік: ${album.releaseYear}`;

        const artistName = document.createElement('p');
        artistName.textContent = `Артист: ${album.artistName || 'Невідомий артист'}`;

        const actions = document.createElement('div');
        actions.className = 'actions';

        const editButton = document.createElement('button');
        editButton.className = 'edit-button';
        editButton.innerHTML = '<i class="fas fa-edit"></i>';
        editButton.setAttribute('data-tooltip', 'Редагувати');
        editButton.setAttribute('onclick', `displayEditForm(${album.id})`);

        const deleteButton = document.createElement('button');
        deleteButton.className = 'delete-button';
        deleteButton.innerHTML = '<i class="fas fa-trash"></i>';
        deleteButton.setAttribute('data-tooltip', 'Видалити');
        deleteButton.setAttribute('onclick', `deleteAlbum(${album.id})`);

        const rateButton = document.createElement('button');
        rateButton.className = 'rate-button';
        rateButton.innerHTML = '<i class="fas fa-star"></i>';
        rateButton.setAttribute('data-tooltip', 'Оцінити');
        rateButton.setAttribute('onclick', `openRateModal(${album.id})`);

        const favoriteButton = document.createElement('button');
        favoriteButton.className = 'favorite-button';
        const favorite = favorites.find(f => f.albumId === album.id);
        const isFavorite = !!favorite;
        const favoriteId = favorite ? favorite.id : null;
        favoriteButton.innerHTML = isFavorite ? '<i class="fas fa-heart"></i>' : '<i class="far fa-heart"></i>';
        favoriteButton.setAttribute('data-tooltip', isFavorite ? 'Видалити з улюблених' : 'Додати до улюблених');
        favoriteButton.setAttribute('onclick', `toggleFavorite(${album.id}, ${isFavorite}, ${favoriteId})`);

        actions.appendChild(editButton);
        actions.appendChild(deleteButton);
        actions.appendChild(rateButton);
        actions.appendChild(favoriteButton);

        const albumInfo = document.createElement('div');
        albumInfo.className = 'album-info';
        albumInfo.setAttribute('onclick', `displayAlbumDetails(${album.id})`);
        albumInfo.appendChild(image);
        albumInfo.appendChild(title);
        albumInfo.appendChild(year);
        albumInfo.appendChild(artistName);

        card.appendChild(albumInfo);
        card.appendChild(actions);

        container.appendChild(card);
    });
}

async function toggleFavorite(albumId, isFavorite, favoriteId) {
    const userId = 1; // Замініть на динамічний userId, якщо автентифікація налаштована
    const method = isFavorite ? 'DELETE' : 'POST';
    const url = `/api/Albums/${albumId}/favorite?userId=${userId}`;

    try {
        const response = await fetch(url, { method });
        if (response.ok) {
            console.log(isFavorite ? 'Альбом видалено з улюблених' : 'Альбом додано до улюблених');
            getAlbums();
        } else {
            const errorText = await response.text();
            console.error(`Помилка при оновленні улюблених: ${response.status} ${errorText}`);
            throw new Error(errorText);
        }
    } catch (error) {
        console.error('Помилка при оновленні улюблених:', error);
        alert('Помилка при оновленні улюблених: ' + error.message);
    }
}

async function addToFavorites(albumId) {
    console.warn('Використовуйте toggleFavorite замість addToFavorites');
    toggleFavorite(albumId, false, null);
}

document.addEventListener('DOMContentLoaded', () => {
    getAlbums();
    setupImagePreview('add-coverImage', 'add-image-preview');
    setupImagePreview('edit-coverImage', 'edit-image-preview');
});

function setupImagePreview(inputId, previewContainerId) {
    const input = document.getElementById(inputId);
    const container = document.getElementById(previewContainerId);

    input.addEventListener('change', function (e) {
        container.innerHTML = '';
        if (e.target.files[0]) {
            const preview = document.createElement('img');
            preview.className = 'image-preview';
            preview.src = URL.createObjectURL(e.target.files[0]);
            container.appendChild(preview);
        }
    });
}

function openRateModal(albumId) {
    const userId = 1; // Замініть на динамічний userId, якщо автентифікація налаштована
    checkFavoritesForUser(userId).then(favorites => {
        const favorite = favorites.find(f => f.albumId === albumId);
        if (!favorite) {
            alert('Спочатку додайте альбом до улюблених.');
            return;
        }

        const rating = prompt('Введіть оцінку (1-5):', '5');
        if (rating === null) return;

        const ratingValue = parseInt(rating);
        if (isNaN(ratingValue) || ratingValue < 1 || ratingValue > 5) {
            alert('Оцінка має бути числом від 1 до 5.');
            return;
        }

        fetch(`/api/FavoriteAlbums/rate/${favorite.id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(ratingValue)
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Помилка HTTP! Статус: ${response.status}`);
                }
                alert('Оцінку збережено!');
            })
            .catch(error => {
                console.error('Не вдалося оцінити альбом:', error);
                alert('Не вдалося оцінити альбом.');
            });
    });
}