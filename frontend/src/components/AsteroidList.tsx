import type { Asteroid } from '../models/asteroid'

const numberFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 0 })
const diameterFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 1 })

interface AsteroidListProps {
  asteroids: Asteroid[]
}

export function AsteroidList({ asteroids }: AsteroidListProps) {
  const hazardousCount = asteroids.filter((a) => a.isPotentiallyHazardous).length

  return (
    <div className="asteroids">
      <p className="asteroids-summary">
        Объектов: {asteroids.length}
        {hazardousCount > 0 && <> · потенциально опасных: <strong className="hazard-text">{hazardousCount}</strong></>}
      </p>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th scope="col">Название</th>
              <th scope="col" className="num">Диаметр, м (мин–макс)</th>
              <th scope="col" className="num">Скорость, км/ч</th>
              <th scope="col" className="num">Расстояние до Земли, км</th>
              <th scope="col">Опасность</th>
            </tr>
          </thead>
          <tbody>
            {asteroids.map((asteroid) => (
              <tr key={asteroid.id} className={asteroid.isPotentiallyHazardous ? 'hazardous' : undefined}>
                <th scope="row">{asteroid.name}</th>
                <td className="num">
                  {diameterFormat.format(asteroid.estimatedDiameterMinMeters)} –{' '}
                  {diameterFormat.format(asteroid.estimatedDiameterMaxMeters)} м
                </td>
                <td className="num">{numberFormat.format(asteroid.relativeVelocityKph)} км/ч</td>
                <td className="num">{numberFormat.format(asteroid.missDistanceKm)} км</td>
                <td>
                  {asteroid.isPotentiallyHazardous ? (
                    <span className="badge badge-hazard">⚠ Потенциально опасен</span>
                  ) : (
                    <span className="badge badge-safe">Не опасен</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
