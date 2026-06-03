import { useEffect, useMemo, useState } from 'react';
import {
  activarColaborador,
  createColaborador,
  desactivarColaborador,
  getColaboradorById,
  getColaboradores,
  updateColaborador,
} from '../../api/colaboradoresApi';
import {
  getCargosCatalogo,
  getDepartamentosCatalogo,
  getEmpresasCatalogo,
  getEstatusColaboradorCatalogo,
  getMotivosSalidaCatalogo,
  getTiposContratoCatalogo,
} from '../../api/catalogosApi';
import { ApiError } from '../../api/httpClient';
import { ColaboradorDetailModal } from '../../components/colaboradores/ColaboradorDetailModal';
import {
  ColaboradorFormModal,
  detailToFormValues,
  emptyColaboradorFormValues,
} from '../../components/colaboradores/ColaboradorFormModal';
import { ColaboradorFilters } from '../../components/colaboradores/ColaboradorFilters';
import { ColaboradoresTable } from '../../components/colaboradores/ColaboradoresTable';
import { ConfirmDialog } from '../../components/colaboradores/ConfirmDialog';
import type { CatalogosColaborador } from '../../types/catalogos';
import type {
  ColaboradorDetail,
  ColaboradorFilterValues,
  ColaboradorFilters as ColaboradorApiFilters,
  ColaboradorFormValues,
  ColaboradorList,
  ColaboradorRequest,
} from '../../types/colaborador';

const defaultFilters: ColaboradorFilterValues = {
  empresaId: '',
  departamentoId: '',
  cargoId: '',
  estatusId: '',
  tipoContratoId: '',
  isActive: 'all',
  search: '',
};

const emptyCatalogos: CatalogosColaborador = {
  empresas: [],
  departamentos: [],
  cargos: [],
  tiposContrato: [],
  estatus: [],
  motivosSalida: [],
};

type FormState =
  | {
      mode: 'create';
      initialValues: ColaboradorFormValues;
      editingId: null;
    }
  | {
      mode: 'edit';
      initialValues: ColaboradorFormValues;
      editingId: number;
    };

type ConfirmState = {
  colaborador: ColaboradorList;
  action: 'activar' | 'desactivar';
};

function toNumber(value: string) {
  return value ? Number(value) : undefined;
}

function toApiFilters(filters: ColaboradorFilterValues): ColaboradorApiFilters {
  return {
    empresaId: toNumber(filters.empresaId),
    departamentoId: toNumber(filters.departamentoId),
    cargoId: toNumber(filters.cargoId),
    estatusId: toNumber(filters.estatusId),
    tipoContratoId: toNumber(filters.tipoContratoId),
    isActive: filters.isActive === 'all' ? undefined : filters.isActive === 'true',
    search: filters.search,
  };
}

function getErrorMessages(error: unknown) {
  if (error instanceof ApiError) {
    return error.errors.length > 0 ? error.errors : [error.message];
  }

  if (error instanceof Error) {
    return [error.message];
  }

  return ['No fue posible completar la operacion.'];
}

export function ColaboradoresPage() {
  const [catalogos, setCatalogos] = useState<CatalogosColaborador>(emptyCatalogos);
  const [colaboradores, setColaboradores] = useState<ColaboradorList[]>([]);
  const [colaboradoresActivos, setColaboradoresActivos] = useState<ColaboradorList[]>([]);
  const [filters, setFilters] = useState<ColaboradorFilterValues>(defaultFilters);
  const [isLoading, setIsLoading] = useState(true);
  const [isCatalogosLoading, setIsCatalogosLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isToggling, setIsToggling] = useState(false);
  const [pageErrors, setPageErrors] = useState<string[]>([]);
  const [formErrors, setFormErrors] = useState<string[]>([]);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [detail, setDetail] = useState<ColaboradorDetail | null>(null);
  const [formState, setFormState] = useState<FormState | null>(null);
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null);

  const totals = useMemo(
    () => ({
      total: colaboradores.length,
      activos: colaboradores.filter((item) => item.isActive).length,
      inactivos: colaboradores.filter((item) => !item.isActive).length,
    }),
    [colaboradores],
  );

  const loadCatalogos = async () => {
    setIsCatalogosLoading(true);
    setPageErrors([]);

    try {
      const [
        empresas,
        departamentos,
        cargos,
        tiposContrato,
        estatus,
        motivosSalida,
      ] = await Promise.all([
        getEmpresasCatalogo(),
        getDepartamentosCatalogo(),
        getCargosCatalogo(),
        getTiposContratoCatalogo(),
        getEstatusColaboradorCatalogo(),
        getMotivosSalidaCatalogo(),
      ]);

      setCatalogos({
        empresas: empresas.data ?? [],
        departamentos: departamentos.data ?? [],
        cargos: cargos.data ?? [],
        tiposContrato: tiposContrato.data ?? [],
        estatus: estatus.data ?? [],
        motivosSalida: motivosSalida.data ?? [],
      });
    } catch (error) {
      setPageErrors(getErrorMessages(error));
    } finally {
      setIsCatalogosLoading(false);
    }
  };

  const loadColaboradores = async (currentFilters = filters) => {
    setIsLoading(true);
    setPageErrors([]);

    try {
      const [listResponse, activeResponse] = await Promise.all([
        getColaboradores(toApiFilters(currentFilters)),
        getColaboradores({ isActive: true }),
      ]);

      setColaboradores(listResponse.data ?? []);
      setColaboradoresActivos(activeResponse.data ?? []);
    } catch (error) {
      setPageErrors(getErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    document.title = 'Colaboradores | Portal RRHH FZ';
    void loadCatalogos();
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadColaboradores(filters);
    }, 250);

    return () => window.clearTimeout(timeoutId);
  }, [filters]);

  const openCreate = () => {
    setFormErrors([]);
    setSuccessMessage(null);
    setFormState({
      mode: 'create',
      initialValues: emptyColaboradorFormValues,
      editingId: null,
    });
  };

  const openEdit = async (id: number) => {
    setFormErrors([]);
    setPageErrors([]);
    setSuccessMessage(null);

    try {
      const response = await getColaboradorById(id);

      if (!response.data) {
        setPageErrors(['No fue posible cargar el colaborador seleccionado.']);
        return;
      }

      setFormState({
        mode: 'edit',
        initialValues: detailToFormValues(response.data),
        editingId: id,
      });
    } catch (error) {
      setPageErrors(getErrorMessages(error));
    }
  };

  const openDetail = async (id: number) => {
    setPageErrors([]);

    try {
      const response = await getColaboradorById(id);

      if (response.data) {
        setDetail(response.data);
      }
    } catch (error) {
      setPageErrors(getErrorMessages(error));
    }
  };

  const handleSubmit = async (request: ColaboradorRequest) => {
    if (!formState) {
      return;
    }

    setIsSaving(true);
    setFormErrors([]);
    setSuccessMessage(null);

    try {
      if (formState.mode === 'create') {
        await createColaborador(request);
        setSuccessMessage('Colaborador creado correctamente.');
      } else {
        await updateColaborador(formState.editingId, request);
        setSuccessMessage('Colaborador actualizado correctamente.');
      }

      setFormState(null);
      await loadColaboradores(filters);
    } catch (error) {
      setFormErrors(getErrorMessages(error));
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleActive = async () => {
    if (!confirmState) {
      return;
    }

    setIsToggling(true);
    setPageErrors([]);
    setSuccessMessage(null);

    try {
      if (confirmState.action === 'activar') {
        await activarColaborador(confirmState.colaborador.colaboradorId);
        setSuccessMessage('Colaborador activado correctamente.');
      } else {
        await desactivarColaborador(confirmState.colaborador.colaboradorId);
        setSuccessMessage('Colaborador desactivado correctamente.');
      }

      setConfirmState(null);
      await loadColaboradores(filters);
    } catch (error) {
      setPageErrors(getErrorMessages(error));
    } finally {
      setIsToggling(false);
    }
  };

  const handleFilterChange = (nextFilters: ColaboradorFilterValues) => {
    setFilters(nextFilters);
    setSuccessMessage(null);
  };

  return (
    <div className="colaboradores-page">
      <section className="page-heading">
        <div>
          <span className="eyebrow">Modulo V1</span>
          <h2>Colaboradores</h2>
        </div>
        <button
          className="primary-button"
          disabled={isCatalogosLoading}
          type="button"
          onClick={openCreate}
        >
          Nuevo colaborador
        </button>
      </section>

      <section className="summary-strip" aria-label="Resumen de colaboradores">
        <article>
          <span>Total listado</span>
          <strong>{totals.total}</strong>
        </article>
        <article>
          <span>Activos</span>
          <strong>{totals.activos}</strong>
        </article>
        <article>
          <span>Inactivos</span>
          <strong>{totals.inactivos}</strong>
        </article>
      </section>

      {successMessage && <div className="success-message">{successMessage}</div>}
      {pageErrors.length > 0 && (
        <div className="form-error-list">
          {pageErrors.map((error) => (
            <span key={error}>{error}</span>
          ))}
        </div>
      )}

      <ColaboradorFilters
        catalogos={catalogos}
        filters={filters}
        onChange={handleFilterChange}
        onClear={() => setFilters(defaultFilters)}
      />

      <ColaboradoresTable
        colaboradores={colaboradores}
        isLoading={isLoading || isCatalogosLoading}
        onEdit={openEdit}
        onToggleActive={(colaborador) =>
          setConfirmState({
            colaborador,
            action: colaborador.isActive ? 'desactivar' : 'activar',
          })
        }
        onView={openDetail}
      />

      {formState && (
        <ColaboradorFormModal
          apiErrors={formErrors}
          catalogos={catalogos}
          colaboradoresActivos={colaboradoresActivos}
          editingId={formState.editingId}
          initialValues={formState.initialValues}
          isSubmitting={isSaving}
          mode={formState.mode}
          onClose={() => setFormState(null)}
          onSubmit={handleSubmit}
        />
      )}

      {detail && <ColaboradorDetailModal colaborador={detail} onClose={() => setDetail(null)} />}

      {confirmState && (
        <ConfirmDialog
          confirmLabel={confirmState.action === 'activar' ? 'Activar' : 'Desactivar'}
          isBusy={isToggling}
          message={`Confirma que deseas ${confirmState.action} a ${confirmState.colaborador.nombreCompleto}.`}
          title={
            confirmState.action === 'activar'
              ? 'Activar colaborador'
              : 'Desactivar colaborador'
          }
          onCancel={() => setConfirmState(null)}
          onConfirm={handleToggleActive}
        />
      )}
    </div>
  );
}
