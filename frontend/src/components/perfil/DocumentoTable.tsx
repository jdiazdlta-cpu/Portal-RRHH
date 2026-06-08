import { ActionsMenu } from '../common/ActionsMenu';
import type { DocumentoColaboradorList } from '../../types/documento';

type DocumentoTableProps = {
  documentos: DocumentoColaboradorList[];
  onDownload: (documento: DocumentoColaboradorList) => void;
  onEdit: (documento: DocumentoColaboradorList) => void;
  onDeactivate: (documento: DocumentoColaboradorList) => void;
};

function formatDate(value: string | null) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('es-PA').format(new Date(value));
}

export function DocumentoTable({
  documentos,
  onDeactivate,
  onDownload,
  onEdit,
}: DocumentoTableProps) {
  return (
    <div className="table-scroll">
      <table className="data-table compact-table">
        <thead>
          <tr>
            <th>Tipo</th>
            <th>Archivo</th>
            <th>Fecha carga</th>
            <th>Vencimiento</th>
            <th>Tiene venc.</th>
            <th>Observacion</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {documentos.length === 0 && (
            <tr>
              <td colSpan={8}>Sin documentos registrados.</td>
            </tr>
          )}
          {documentos.map((documento) => (
            <tr key={documento.documentoColaboradorId}>
              <td>{documento.tipoDocumentoNombre}</td>
              <td>{documento.nombreArchivo}</td>
              <td>{formatDate(documento.fechaCarga)}</td>
              <td>{formatDate(documento.fechaVencimiento)}</td>
              <td>{documento.tieneVencimiento ? 'Si' : 'No'}</td>
              <td>{documento.observacion ?? '-'}</td>
              <td>
                <span className={documento.isActive ? 'status active' : 'status inactive'}>
                  {documento.isActive ? 'Activo' : 'Inactivo'}
                </span>
              </td>
              <td>
                <ActionsMenu
                  items={[
                    { label: 'Descargar', onClick: () => onDownload(documento) },
                    { label: 'Editar metadata', onClick: () => onEdit(documento) },
                    {
                      disabled: !documento.isActive,
                      label: 'Desactivar',
                      onClick: () => onDeactivate(documento),
                    },
                  ]}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
