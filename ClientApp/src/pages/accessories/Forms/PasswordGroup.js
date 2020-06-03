import React, { useState } from "react";
import { Modal } from "react-bootstrap";
import validator from "../../../utils/validator";
import LocalizedStrings from "react-localization";

const text = new LocalizedStrings({
    en: {
        title: "Change password",
        label: "Password",
        newholder: "New password",
        change: "Change",
        confirm: "Confirm",
        clear: "Clear",
    },
    ru: {
        title: "Изменить пароль",
        label: "Пароль",
        newholder: "Новый пароль",
        change: "Изменить",
        confirm: "Подтвердить",
        clear: "Очистить",
    }
})

export function PasswordGroup(props) {
    const [showModal, setShowModal] = useState(false);
    const [newpassword, setNewPassword] = useState(props.newpassword);
    const [error, setError] = useState(() => (validator.password(props.newpassword, props.lang)));
    const show = () => {
        setNewPassword(props.newpassword);
        setError(validator.password(props.newpassword, props.lang));
    }
    const hide = () => {
        setShowModal(false)
    }
    const passwordChanged = ev => {
        let val = ev.target.value;
        setNewPassword(val);
        setError(validator.password(val, props.lang));
    }
    const clearInputs = () => {
        setNewPassword("");
        setError(validator.password("", props.lang));
    }
    const confirmChange = () => {
        if (!error || !newpassword) {
            props.onChange(newpassword);
            setShowModal(false);
        }
    }
    text.setLanguage(props.lang);
    return <>
        <div className="form-group">
            <div className="row pb-4">
                <div className="col-3">
                    <label className="h5">{text.label}</label>
                </div>
                <div className="col">
                    <div className="input-group">
                        <div className="input-group-prepend">
                            <span className="input-group-text"><strong className="invisible">&ndash;</strong></span>
                        </div>
                        <button onClick={() => setShowModal(true)} className="btn btn-outline-secondary">{text.change}</button>
                    </div>
                </div>
            </div>
        </div>
        <Modal id="password_modal" show={showModal} onHide={hide} onShow={show} >
            <Modal.Header closeButton>
                <Modal.Title>{text.title}</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <div className="form-group">
                    <input type="text" className="form-control" placeholder={text.newholder}
                        value={newpassword} onChange={passwordChanged} />
                    <p className="text-danger">{error}</p>
                </div>
            </Modal.Body>
            <Modal.Footer>
                <button className="btn btn-outline-secondary" onClick={clearInputs}>{text.clear}</button>
                <button className="btn btn-info" onClick={confirmChange}>{text.confirm}</button>
            </Modal.Footer>
        </Modal>
    </>
}