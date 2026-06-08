import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getHistorialColaborador, getColaboradorPerfil } from '../../api/colaboradoresApi';
import { getTiposDocumentoCatalogo } from '../../api/catalogosApi';
import {
  descargarDocumento,
  desactivarDocumento,
  getDocumentoById,
  getDocumentosColaborador,
  updateDocumento,
  uploadDocumento,
} from '../../api/documentosApi';
import { ApiError } from '../../api/httpClient';
import { ConfirmDialog } from '../../components/colaboradores/ConfirmDialog';
import { DocumentoEditModal } from '../../components/perfil/DocumentoEditModal';
import { DocumentoTable } from '../../components/perfil/DocumentoTable';
import { DocumentoUploadModal } from '../../components/perfil/DocumentoUploadModal';
import { HistorialColaboradorTable } from '../../components/perfil/HistorialColaboradorTable';
import {
  PerfilCompensacion,
  PerfilContrato,
  PerfilDatosLaborales,
  PerfilDatosPersonales,
  PerfilResumenCard,
  PerfilVencimientos,
} from '../../components/perfil/PerfilCards';
import type { TipoDocumentoCatalogo } from '../../types/catalogos';
import type { ColaboradorPerfil, HistorialColaborador } from '../../types/colaborador';
import type {
  DocumentoColaboradorDetail,
  DocumentoColaboradorList,
  UpdateDocumentoRequest,
  UploadDocumentoRequest,
} from '../../types/documento';

function getErrorMessages(error: unknown) {
  if (error instanceof ApiError) {
    return error.errors.length > 0 ? error.errors : [error.message];
  }

  if (error instanceof Error) {
    return [error.message];
  }

  return ['No fue posible completar la operacion.'];
}

function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export function ColaboradorPerfilPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const colaboradorId = Number(id);
  const [perfil, setPerfil] = useState<ColaboradorPerfil | null>(null);
  const [historial, setHistorial] = useState<HistorialColaborador[]>([]);
  const [documentos, setDocumentos] = useState<DocumentoColaboradorList[]>([]);
  const [tiposDocumento, setTiposDocumento] = useState<TipoDocumentoCatalogo[]>([]);
  const [editDocumento, setEditDocumento] = useState<DocumentoColaboradorDetail | null>(null);
  const [confirmDocumento, setConfirmDocumento] = useState<DocumentoColaboradorList | null>(null);
  const [showUpload, setShowUpload] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isDownloading, setIsDownloading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [modalErrors, setModalErrors] = useState<string[]>([]);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadPerfil = async () => {
    if (!colaboradorId) {
      setErrors(['Colaborador invalido.']);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrors([]);

    try {
      const [perfilResponse, historialResponse, documentosResponse, tiposResponse] =
        await Promise.all([
          getColaboradorPerfil(colaboradorId),
          getHistorialColaborador(colaboradorId),
          getDocumentosColaborador(colaboradorId),
          getTiposDocumentoCatalogo(),
        ]);

      setPerfil(perfilResponse.data);
      setHistorial(historialResponse.data ?? []);
      setDocumentos(documentosResponse.data ?? []);
      setTiposDocumento(tiposResponse.data ?? []);
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  const reloadDocumentosHistorial = async () => {
    const [documentosResponse, historialResponse] = await Promise.all([
      getDocumentosColaborador(colaboradorId),
      getHistorialColaborador(colaboradorId),
    ]);
    setDocumentos(documentosResponse.data ?? []);
    setHistorial(historialResponse.data ?? []);
  };

  useEffect(() => {
    document.title = 'Perfil colaborador | Portal RRHH FZ';
    void loadPerfil();
  }, [colaboradorId]);

  const handleUpload = async (request: UploadDocumentoRequest) => {
    setIsSaving(true);
    setModalErrors([]);
    setSuccessMessage(null);

    try {
      await uploadDocumento(colaboradorId, request);
      setShowUpload(false);
      setSuccessMessage('Documento subido correctamente.');
      await reloadDocumentosHistorial();
    } catch (error) {
      setModalErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  const handleOpenEdit = async (documento: DocumentoColaboradorList) => {
    setModalErrors([]);
    setErrors([]);

    try {
      const response = await getDocumentoById(documento.documentoColaboradorId);

      if (response.data) {
        setEditDocumento(response.data);
      }
    } catch (error) {
      setErrors(getErrorMessages(error));
    }
  };

  const handleEdit = async (request: UpdateDocumentoRequest) => {
    if (!editDocumento) {
      return;
    }

    setIsSaving(true);
    setModalErrors([]);
    setSuccessMessage(null);

    try {
      await updateDocumento(editDocumento.documentoColaboradorId, request);
      setEditDocumento(null);
      setSuccessMessage('Metadata del documento actualizada correctamente.');
      await reloadDocumentosHistorial();
    } catch (error) {
      setModalErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDownload = async (documento: DocumentoColaboradorList) => {
    setIsDownloading(true);
    setErrors([]);

    try {
      const response = await descargarDocumento(documento.documentoColaboradorId);
      saveBlob(response.blob, response.fileName ?? documento.nombreArchivo);
      setSuccessMessage('Descarga iniciada.');
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsDownloading(false);
    }
  };

  const handleDeactivate = async () => {
    if (!confirmDocumento) {
      return;
    }

    setIsSaving(true);
    setErrors([]);
    setSuccessMessage(null);

    try {
      await desactivarDocumento(confirmDocumento.documentoColaboradorId);
      setConfirmDocumento(null);
      setSuccessMessage('Documento desactivado correctamente.');
      await reloadDocumentosHistorial();
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <section className="state-panel">
        <div className="loader" />
        <h2>Cargando perfil</h2>
      </section>
    );
  }

  if (!perfil) {
    return (
      <section className="state-panel">
        <h2>No se pudo cargar el perfil</h2>
        {errors.map((error) => (
          <p key={error}>{error}</p>
        ))}
        <button className="secondary-button compact-button" type="button" onClick={() => navigate('/colaboradores')}>
          Volver
        </button>
      </section>
    );
  }

  return (
    <div className="perfil-page">
      <section className="page-heading">
        <div>
          <span className="eyebrow">Colaboradores</span>
          <h2>Perfil</h2>
        </div>
        <Link className="secondary-button" to="/colaboradores">
          Volver a colaboradores
        </Link>
      </section>

      {successMessage && <div className="success-message">{successMessage}</div>}
      {errors.length > 0 && (
        <div className="form-error-list">
          {errors.map((error) => (
            <span key={error}>{error}</span>
          ))}
        </div>
      )}

      <PerfilResumenCard perfil={perfil} />

      <section className="profile-grid">
        <PerfilDatosPersonales perfil={perfil} />
        <PerfilDatosLaborales perfil={perfil} />
        <PerfilContrato perfil={perfil} />
        <PerfilVencimientos perfil={perfil} />
        <PerfilCompensacion perfil={perfil} />
        <section className="profile-card">
          <h3>Estado</h3>
          <div className="detail-row">
            <span>Estatus</span>
            <strong>{perfil.datosLaborales.estatusNombre}</strong>
          </div>
          <div className="detail-row">
            <span>Activo</span>
            <strong>{perfil.datosLaborales.isActive ? 'Si' : 'No'}</strong>
          </div>
        </section>
      </section>

      <section className="panel wide-panel">
        <header className="panel-header">
          <div>
            <h3>Expediente digital</h3>
            <span className="muted">{documentos.length} documentos</span>
          </div>
          <button className="primary-button compact-button" type="button" onClick={() => setShowUpload(true)}>
            Subir documento
          </button>
        </header>
        {isDownloading && <div className="hint-text">Preparando descarga...</div>}
        <DocumentoTable
          documentos={documentos}
          onDeactivate={setConfirmDocumento}
          onDownload={handleDownload}
          onEdit={handleOpenEdit}
        />
      </section>

      <HistorialColaboradorTable historial={historial} />

      {showUpload && (
        <DocumentoUploadModal
          apiErrors={modalErrors}
          isSubmitting={isSaving}
          tiposDocumento={tiposDocumento}
          onClose={() => setShowUpload(false)}
          onSubmit={handleUpload}
        />
      )}

      {editDocumento && (
        <DocumentoEditModal
          apiErrors={modalErrors}
          documento={editDocumento}
          isSubmitting={isSaving}
          tiposDocumento={tiposDocumento}
          onClose={() => setEditDocumento(null)}
          onSubmit={handleEdit}
        />
      )}

      {confirmDocumento && (
        <ConfirmDialog
          confirmLabel="Desactivar"
          isBusy={isSaving}
          message={`Confirma que deseas desactivar ${confirmDocumento.nombreArchivo}.`}
          title="Desactivar documento"
          onCancel={() => setConfirmDocumento(null)}
          onConfirm={handleDeactivate}
        />
      )}
    </div>
  );
}
