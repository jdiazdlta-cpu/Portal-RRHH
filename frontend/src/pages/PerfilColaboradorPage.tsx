import { FormEvent, useEffect, useState } from 'react';
import { Download, RefreshCw, Upload } from 'lucide-react';
import { useParams } from 'react-router-dom';
import { apiGet, apiPost, downloadFile } from '../api/client';
import type { CatalogoItem, ColaboradorDetalle } from '../types/api';
import { formatDate, formatMoney, statusClass } from '../utils/format';

export function PerfilColaboradorPage() {
  const { id } = useParams();
  const [data, setData] = useState<ColaboradorDetalle | null>(null);
  const [tiposDocumento, setTiposDocumento] = useState<CatalogoItem[]>([]);
  const [tipoDocumentoId, setTipoDocumentoId] = useState('');
  const [archivo, setArchivo] = useState<File | null>(null);
  const [tieneVencimiento, setTieneVencimiento] = useState(false);
  const [fechaVencimiento, setFechaVencimiento] = useState('');
  const [observacion, setObservacion] = useState('');
  const [error, setError] = useState('');
  const [uploading, setUploading] = useState(false);

  const load = () => {
    if (!id) return;
    Promise.all([
      apiGet<ColaboradorDetalle>(`/colaboradores/${id}/perfil`),
      apiGet<CatalogoItem[]>('/catalogos/tipos-documento')
    ])
      .then(([profile, docs]) => {
        setData(profile);
        setTiposDocumento(docs);
        setTipoDocumentoId((current) => current || String(docs[0]?.id ?? ''));
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'No se pudo cargar el perfil.'));
  };

  useEffect(load, [id]);

  async function upload(event: FormEvent) {
    event.preventDefault();
    if (!id || !archivo || !tipoDocumentoId) return;
    setUploading(true);
    setError('');
    const form = new FormData();
    form.append('Archivo', archivo);
    form.append('TipoDocumentoId', tipoDocumentoId);
    form.append('TieneVencimiento', String(tieneVencimiento));
    if (fechaVencimiento) form.append('FechaVencimiento', fechaVencimiento);
    if (observacion) form.append('Observacion', observacion);
    try {
      await apiPost(`/colaboradores/${id}/documentos`, form);
      setArchivo(null);
      setObservacion('');
      setFechaVencimiento('');
      setTieneVencimiento(false);
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo cargar el documento.');
    } finally {
      setUploading(false);
    }
  }

  if (!data) {
    return <div className="screen-loader">Cargando perfil</div>;
  }

  return (
    <section className="page">
      <div className="page-heading">
        <div>
          <h1>{data.nombreCompleto}</h1>
          <p>{data.noEmpleado} · {data.cedula}</p>
        </div>
        <span className={`badge ${statusClass(data.estatus)}`}>{data.estatus}</span>
      </div>
      {error && <div className="error-box">{error}</div>}

      <div className="profile-grid">
        <section className="panel">
          <h2>Datos personales</h2>
          <dl className="detail-list">
            <div><dt>Telefono</dt><dd>{data.telefono ?? 'N/D'}</dd></div>
            <div><dt>Email</dt><dd>{data.email ?? 'N/D'}</dd></div>
            <div><dt>Nacimiento</dt><dd>{formatDate(data.fechaNacimiento)}</dd></div>
            <div><dt>Direccion</dt><dd>{data.direccion ?? 'N/D'}</dd></div>
          </dl>
        </section>
        <section className="panel">
          <h2>Datos laborales</h2>
          <dl className="detail-list">
            <div><dt>Empresa</dt><dd>{data.empresa}</dd></div>
            <div><dt>Departamento</dt><dd>{data.departamento}</dd></div>
            <div><dt>Cargo</dt><dd>{data.cargo}</dd></div>
            <div><dt>Ingreso</dt><dd>{formatDate(data.fechaIngreso)}</dd></div>
            <div><dt>Contrato</dt><dd>{data.tipoContrato}</dd></div>
            <div><dt>Jefe</dt><dd>{data.jefeInmediato ?? 'N/D'}</dd></div>
          </dl>
        </section>
        <section className="panel">
          <h2>Compensacion</h2>
          <dl className="detail-list">
            <div><dt>Salario</dt><dd>{formatMoney(data.salario)}</dd></div>
            <div><dt>Viaticos</dt><dd>{formatMoney(data.viaticos)}</dd></div>
            <div><dt>Gastos</dt><dd>{formatMoney(data.gastosRepresentacion)}</dd></div>
          </dl>
        </section>
        <section className="panel">
          <h2>Vencimientos</h2>
          <dl className="detail-list">
            <div><dt>Cedula</dt><dd>{formatDate(data.fechaVencimientoCedula)}</dd></div>
            <div><dt>Contrato</dt><dd>{formatDate(data.fechaVencimientoContrato)}</dd></div>
            <div><dt>Periodo probatorio</dt><dd>{formatDate(data.fechaVencimientoPeriodoProbatorio)}</dd></div>
            <div><dt>Licencia</dt><dd>{formatDate(data.fechaVencimientoLicencia)}</dd></div>
          </dl>
        </section>
      </div>

      <section className="panel">
        <div className="panel-title-row">
          <h2>Expediente digital</h2>
          <button className="icon-button" onClick={load} title="Actualizar" aria-label="Actualizar"><RefreshCw size={18} /></button>
        </div>
        <form className="upload-row" onSubmit={upload}>
          <label>
            Tipo
            <select value={tipoDocumentoId} onChange={(event) => setTipoDocumentoId(event.target.value)}>
              {tiposDocumento.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
            </select>
          </label>
          <label>
            Archivo
            <input type="file" onChange={(event) => setArchivo(event.target.files?.[0] ?? null)} />
          </label>
          <label className="check-label">
            <input type="checkbox" checked={tieneVencimiento} onChange={(event) => setTieneVencimiento(event.target.checked)} />
            Vence
          </label>
          <label>
            Fecha
            <input type="date" value={fechaVencimiento} onChange={(event) => setFechaVencimiento(event.target.value)} disabled={!tieneVencimiento} />
          </label>
          <label>
            Observacion
            <input value={observacion} onChange={(event) => setObservacion(event.target.value)} />
          </label>
          <button className="primary-button" disabled={uploading || !archivo}>
            <Upload size={18} />
            Cargar
          </button>
        </form>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Archivo</th>
                <th>Carga</th>
                <th>Vencimiento</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {data.documentos.length === 0 && <tr><td colSpan={5}><div className="empty-state">Sin documentos</div></td></tr>}
              {data.documentos.map((doc) => (
                <tr key={doc.documentoColaboradorId}>
                  <td>{doc.tipoDocumento}</td>
                  <td>{doc.nombreArchivo}</td>
                  <td>{formatDate(doc.fechaCarga)}</td>
                  <td>{formatDate(doc.fechaVencimiento)}</td>
                  <td>
                    <button className="icon-text-button" onClick={() => downloadFile(`/documentos/${doc.documentoColaboradorId}/descargar`, doc.nombreArchivo)}>
                      <Download size={16} />
                      Descargar
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </section>
  );
}
